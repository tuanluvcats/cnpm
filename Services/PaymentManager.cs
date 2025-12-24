using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Models;

namespace SanBong.Services;

/// <summary>
/// Factory để tạo Payment Service phù hợp
/// </summary>
public class PaymentServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentService GetPaymentService(string provider)
    {
        return provider.ToLower() switch
        {
            "momo" => _serviceProvider.GetRequiredService<MoMoPaymentService>(),
            "zalopay" => _serviceProvider.GetRequiredService<ZaloPayPaymentService>(),
            _ => throw new ArgumentException($"Unknown payment provider: {provider}")
        };
    }
}

/// <summary>
/// Manager để xử lý thanh toán và lưu trữ giao dịch
/// </summary>
public class PaymentManager
{
    private readonly AppDbContext _context;
    private readonly PaymentServiceFactory _paymentFactory;
    private readonly ILogger<PaymentManager> _logger;

    public PaymentManager(AppDbContext context, PaymentServiceFactory paymentFactory, ILogger<PaymentManager> logger)
    {
        _context = context;
        _paymentFactory = paymentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Tạo thanh toán mới
    /// </summary>
    public async Task<PaymentResult> CreatePaymentAsync(int datSanId, string provider, string returnUrl, string notifyUrl)
    {
        var datSan = await _context.DatSan
            .Include(d => d.MaKhNavigation)
            .Include(d => d.MaSanNavigation)
            .FirstOrDefaultAsync(d => d.MaDatSan == datSanId);

        if (datSan == null)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Không tìm thấy đơn đặt sân"
            };
        }

        // Tạo mã giao dịch nội bộ
        var transactionCode = $"GD{DateTime.Now:yyyyMMddHHmmss}{datSanId}";

        // Tạo bản ghi thanh toán
        var thanhToan = new ThanhToan
        {
            MaDatSan = datSanId,
            PhuongThuc = provider,
            SoTien = datSan.TongTien ?? 0,
            TrangThai = "ChoDuyet",
            MaGiaoDich = transactionCode
        };

        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        // Tạo giao dịch thanh toán online
        var giaoDich = new GiaoDichThanhToan
        {
            MaThanhToan = thanhToan.MaTt,
            MaGiaoDichCode = transactionCode,
            NhaCungCap = provider,
            SoTien = datSan.TongTien ?? 0,
            MoTa = $"Thanh toán đặt sân {datSan.MaSanNavigation?.TenSan}",
            ThoiGianTao = DateTime.Now,
            TrangThai = "Pending",
            CallbackUrl = notifyUrl,
            ReturnUrl = returnUrl
        };

        _context.GiaoDichThanhToan.Add(giaoDich);
        await _context.SaveChangesAsync();

        // Gọi API thanh toán
        var paymentService = _paymentFactory.GetPaymentService(provider);
        var request = new PaymentRequest
        {
            OrderId = transactionCode,
            Amount = datSan.TongTien ?? 0,
            Description = $"Thanh toán đặt sân {datSan.MaSanNavigation?.TenSan}",
            ReturnUrl = returnUrl,
            NotifyUrl = notifyUrl,
            CustomerName = datSan.MaKhNavigation?.HoTen,
            CustomerPhone = datSan.MaKhNavigation?.DienThoai,
            CustomerEmail = datSan.MaKhNavigation?.Email,
            ExtraData = new Dictionary<string, string>
            {
                { "datSanId", datSanId.ToString() },
                { "thanhToanId", thanhToan.MaTt.ToString() }
            }
        };

        var result = await paymentService.CreatePaymentAsync(request);

        // Cập nhật thông tin giao dịch
        giaoDich.RequestData = System.Text.Json.JsonSerializer.Serialize(request);
        if (result.Success)
        {
            giaoDich.MaGiaoDichDoiTac = result.TransactionId;
            giaoDich.ResponseData = System.Text.Json.JsonSerializer.Serialize(result.Data);
        }
        else
        {
            giaoDich.TrangThai = "Failed";
            giaoDich.ErrorMessage = result.Message;
            thanhToan.TrangThai = "ThatBai";
        }

        await _context.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Xử lý callback từ cổng thanh toán
    /// </summary>
    public async Task<PaymentResult> HandleCallbackAsync(string provider, Dictionary<string, string> callbackData)
    {
        try
        {
            var paymentService = _paymentFactory.GetPaymentService(provider);
            
            // Lấy orderId từ callback
            var orderId = provider.ToLower() switch
            {
                "momo" => callbackData.GetValueOrDefault("orderId", ""),
                "zalopay" => GetZaloPayOrderId(callbackData),
                _ => ""
            };

            var giaoDich = await _context.GiaoDichThanhToan
                .Include(g => g.MaThanhToanNavigation)
                .FirstOrDefaultAsync(g => g.MaGiaoDichCode == orderId);

            if (giaoDich == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "Không tìm thấy giao dịch"
                };
            }

            var result = await paymentService.VerifyPaymentAsync(orderId, callbackData);

            // Cập nhật trạng thái
            giaoDich.ThoiGianCapNhat = DateTime.Now;
            giaoDich.ResponseData = System.Text.Json.JsonSerializer.Serialize(callbackData);

            if (result.Success)
            {
                giaoDich.TrangThai = "Success";
                giaoDich.MaGiaoDichDoiTac = result.TransactionId;

                if (giaoDich.MaThanhToanNavigation != null)
                {
                    giaoDich.MaThanhToanNavigation.TrangThai = "DaThanhToan";
                    giaoDich.MaThanhToanNavigation.NgayThanhToan = DateTime.Now;

                    // Cập nhật trạng thái đặt sân
                    var datSan = await _context.DatSan.FindAsync(giaoDich.MaThanhToanNavigation.MaDatSan);
                    if (datSan != null)
                    {
                        datSan.TrangThai = "Đã xác nhận";
                    }
                }
            }
            else
            {
                giaoDich.TrangThai = result.Status == PaymentStatus.Cancelled ? "Cancelled" : "Failed";
                giaoDich.ErrorMessage = result.Message;

                if (giaoDich.MaThanhToanNavigation != null)
                {
                    giaoDich.MaThanhToanNavigation.TrangThai = result.Status == PaymentStatus.Cancelled ? "DaHuy" : "ThatBai";
                }
            }

            await _context.SaveChangesAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment callback");
            return new PaymentResult
            {
                Success = false,
                Message = ex.Message,
                Status = PaymentStatus.Failed
            };
        }
    }

    private string GetZaloPayOrderId(Dictionary<string, string> callbackData)
    {
        var dataStr = callbackData.GetValueOrDefault("data", "");
        if (string.IsNullOrEmpty(dataStr)) return "";

        var data = System.Text.Json.JsonSerializer.Deserialize<ZaloPayCallbackData>(dataStr);
        return data?.AppTransId ?? "";
    }
}

using System.Security.Cryptography;
using System.Text;

namespace SanBong.Services;

/// <summary>
/// Sandbox Payment Service - Dùng để demo/test thanh toán
/// Giả lập các cổng thanh toán MoMo, ZaloPay
/// </summary>
public class SandboxPaymentService : IPaymentService
{
    private readonly ILogger<SandboxPaymentService> _logger;
    private static readonly Dictionary<string, SandboxTransaction> _transactions = new();

    public SandboxPaymentService(ILogger<SandboxPaymentService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        var transactionId = GenerateTransactionId();
        
        // Lưu giao dịch vào memory (sandbox)
        var transaction = new SandboxTransaction
        {
            TransactionId = transactionId,
            OrderId = request.OrderId,
            Amount = request.Amount,
            Description = request.Description,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.Now,
            ReturnUrl = request.ReturnUrl,
            NotifyUrl = request.NotifyUrl
        };
        
        _transactions[transactionId] = transaction;

        _logger.LogInformation("Sandbox: Created payment {TransactionId} for order {OrderId}, amount {Amount}", 
            transactionId, request.OrderId, request.Amount);

        // Tạo URL thanh toán sandbox
        var paymentUrl = $"/Payment/SandboxPayment?transactionId={transactionId}";

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            PaymentUrl = paymentUrl,
            Message = "Tạo giao dịch thành công (Sandbox)",
            Status = PaymentStatus.Pending,
            Data = new Dictionary<string, string>
            {
                { "orderId", request.OrderId },
                { "transactionId", transactionId },
                { "amount", request.Amount.ToString() }
            }
        });
    }

    public Task<PaymentResult> VerifyPaymentAsync(string transactionId, Dictionary<string, string> callbackData)
    {
        if (!_transactions.TryGetValue(transactionId, out var transaction))
        {
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                Message = "Không tìm thấy giao dịch",
                Status = PaymentStatus.Failed
            });
        }

        var status = callbackData.GetValueOrDefault("status", "success");
        
        if (status == "success")
        {
            transaction.Status = PaymentStatus.Success;
            transaction.PaidAt = DateTime.Now;
            
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Message = "Thanh toán thành công",
                Status = PaymentStatus.Success,
                Data = new Dictionary<string, string>
                {
                    { "orderId", transaction.OrderId },
                    { "amount", transaction.Amount.ToString() }
                }
            });
        }

        transaction.Status = status == "cancelled" ? PaymentStatus.Cancelled : PaymentStatus.Failed;
        
        return Task.FromResult(new PaymentResult
        {
            Success = false,
            Message = status == "cancelled" ? "Giao dịch đã bị hủy" : "Thanh toán thất bại",
            Status = transaction.Status
        });
    }

    public Task<PaymentResult> CheckPaymentStatusAsync(string transactionId)
    {
        if (!_transactions.TryGetValue(transactionId, out var transaction))
        {
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                Message = "Không tìm thấy giao dịch",
                Status = PaymentStatus.Failed
            });
        }

        return Task.FromResult(new PaymentResult
        {
            Success = transaction.Status == PaymentStatus.Success,
            TransactionId = transactionId,
            Message = GetStatusMessage(transaction.Status),
            Status = transaction.Status,
            Data = new Dictionary<string, string>
            {
                { "orderId", transaction.OrderId },
                { "amount", transaction.Amount.ToString() }
            }
        });
    }

    public Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
    {
        if (!_transactions.TryGetValue(transactionId, out var transaction))
        {
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                Message = "Không tìm thấy giao dịch",
                Status = PaymentStatus.Failed
            });
        }

        if (transaction.Status != PaymentStatus.Success)
        {
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                Message = "Chỉ có thể hoàn tiền giao dịch đã thanh toán thành công",
                Status = PaymentStatus.Failed
            });
        }

        transaction.Status = PaymentStatus.Refunded;
        transaction.RefundedAt = DateTime.Now;
        transaction.RefundAmount = amount;
        transaction.RefundReason = reason;

        _logger.LogInformation("Sandbox: Refunded {Amount} for transaction {TransactionId}", amount, transactionId);

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            Message = $"Đã hoàn tiền {amount:N0}đ",
            Status = PaymentStatus.Refunded
        });
    }

    /// <summary>
    /// Lấy thông tin giao dịch sandbox
    /// </summary>
    public static SandboxTransaction? GetTransaction(string transactionId)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return transaction;
    }

    /// <summary>
    /// Cập nhật trạng thái giao dịch (dùng cho sandbox UI)
    /// </summary>
    public static void UpdateTransactionStatus(string transactionId, PaymentStatus status)
    {
        if (_transactions.TryGetValue(transactionId, out var transaction))
        {
            transaction.Status = status;
            if (status == PaymentStatus.Success)
            {
                transaction.PaidAt = DateTime.Now;
            }
        }
    }

    private string GenerateTransactionId()
    {
        return $"SB{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }

    private string GetStatusMessage(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Đang chờ thanh toán",
        PaymentStatus.Success => "Thanh toán thành công",
        PaymentStatus.Failed => "Thanh toán thất bại",
        PaymentStatus.Cancelled => "Đã hủy",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => "Không xác định"
    };
}

/// <summary>
/// Thông tin giao dịch Sandbox
/// </summary>
public class SandboxTransaction
{
    public string TransactionId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ReturnUrl { get; set; }
    public string? NotifyUrl { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundReason { get; set; }
    public DateTime? RefundedAt { get; set; }
}

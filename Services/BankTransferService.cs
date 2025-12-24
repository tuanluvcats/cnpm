namespace SanBong.Services;

/// <summary>
/// Service tạo QR Code thanh toán ngân hàng (VietQR)
/// Theo chuẩn NAPAS - Hỗ trợ tất cả ngân hàng Việt Nam
/// </summary>
public class BankTransferService
{
    private readonly ILogger<BankTransferService> _logger;

    // Thông tin tài khoản ngân hàng của sân bóng (demo)
    public const string BANK_ID = "970422"; // MB Bank
    public const string ACCOUNT_NO = "0123456789012"; // Số tài khoản demo
    public const string ACCOUNT_NAME = "CTY TNHH SAN BONG ABC";
    public const string BANK_NAME = "MB Bank";

    public BankTransferService(ILogger<BankTransferService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tạo thông tin chuyển khoản
    /// </summary>
    public BankTransferInfo CreateTransferInfo(string orderId, decimal amount, string description)
    {
        var transferCode = GenerateTransferCode(orderId);
        var content = $"{transferCode} {description}";

        // Tạo VietQR URL (chuẩn NAPAS)
        var qrUrl = GenerateVietQRUrl(BANK_ID, ACCOUNT_NO, amount, content, ACCOUNT_NAME);

        _logger.LogInformation("Created bank transfer: {TransferCode}, Amount: {Amount}", transferCode, amount);

        return new BankTransferInfo
        {
            TransferCode = transferCode,
            BankName = BANK_NAME,
            AccountNo = ACCOUNT_NO,
            AccountName = ACCOUNT_NAME,
            Amount = amount,
            Content = content,
            QRCodeUrl = qrUrl,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(15) // Hết hạn sau 15 phút
        };
    }

    /// <summary>
    /// Tạo QR thanh toán tiền cọc (30%)
    /// </summary>
    public BankTransferInfo CreateDepositTransferInfo(string orderId, decimal totalAmount, string description)
    {
        var depositAmount = Math.Ceiling(totalAmount * 0.3m / 1000) * 1000; // Làm tròn lên nghìn
        var remainingAmount = totalAmount - depositAmount;

        var info = CreateTransferInfo(orderId, depositAmount, $"COC {description}");
        info.IsDeposit = true;
        info.DepositPercent = 30;
        info.TotalAmount = totalAmount;
        info.RemainingAmount = remainingAmount;

        return info;
    }

    /// <summary>
    /// Tạo URL VietQR theo chuẩn NAPAS
    /// https://vietqr.io/
    /// </summary>
    private string GenerateVietQRUrl(string bankId, string accountNo, decimal amount, string content, string accountName)
    {
        // Encode các tham số
        var encodedContent = Uri.EscapeDataString(content);
        var encodedName = Uri.EscapeDataString(accountName);

        // VietQR API (miễn phí)
        return $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.jpg?amount={amount:0}&addInfo={encodedContent}&accountName={encodedName}";
    }

    /// <summary>
    /// Tạo mã chuyển khoản duy nhất
    /// </summary>
    private string GenerateTransferCode(string orderId)
    {
        return $"SB{DateTime.Now:ddMMyy}{orderId.GetHashCode():X8}"[..12].ToUpper();
    }

    /// <summary>
    /// Xác minh giao dịch chuyển khoản (giả lập)
    /// Trong thực tế cần tích hợp với API ngân hàng hoặc dịch vụ webhook
    /// </summary>
    public async Task<bool> VerifyTransferAsync(string transferCode, decimal expectedAmount)
    {
        // Giả lập kiểm tra - trong thực tế sẽ gọi API ngân hàng
        await Task.Delay(500);

        _logger.LogInformation("Verifying transfer: {TransferCode}, Expected: {Amount}", transferCode, expectedAmount);

        // Demo: luôn trả về true
        return true;
    }
}

/// <summary>
/// Thông tin chuyển khoản ngân hàng
/// </summary>
public class BankTransferInfo
{
    public string TransferCode { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string AccountNo { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Content { get; set; } = null!;
    public string QRCodeUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Thông tin tiền cọc
    public bool IsDeposit { get; set; }
    public int DepositPercent { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

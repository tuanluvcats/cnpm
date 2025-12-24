namespace SanBong.Services;

/// <summary>
/// Interface cho các dịch vụ thanh toán online
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Tạo yêu cầu thanh toán
    /// </summary>
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);

    /// <summary>
    /// Xác nhận thanh toán từ callback
    /// </summary>
    Task<PaymentResult> VerifyPaymentAsync(string transactionId, Dictionary<string, string> callbackData);

    /// <summary>
    /// Kiểm tra trạng thái giao dịch
    /// </summary>
    Task<PaymentResult> CheckPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Hoàn tiền giao dịch
    /// </summary>
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason);
}

/// <summary>
/// Yêu cầu thanh toán
/// </summary>
public class PaymentRequest
{
    public string OrderId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string NotifyUrl { get; set; } = null!;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public Dictionary<string, string>? ExtraData { get; set; }
}

/// <summary>
/// Kết quả thanh toán
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public PaymentStatus Status { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Trạng thái thanh toán
/// </summary>
public enum PaymentStatus
{
    Pending,
    Success,
    Failed,
    Cancelled,
    Refunded
}

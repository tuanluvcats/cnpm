using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SanBong.Services;

/// <summary>
/// Cấu hình MoMo
/// </summary>
public class MoMoConfig
{
    public string PartnerCode { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string QueryEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/query";
    public string RefundEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/refund";
}

/// <summary>
/// Service thanh toán MoMo
/// Tài liệu: https://developers.momo.vn/v3/docs/payment/api/wallet/onetime
/// </summary>
public class MoMoPaymentService : IPaymentService
{
    private readonly MoMoConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoMoPaymentService> _logger;

    public MoMoPaymentService(MoMoConfig config, HttpClient httpClient, ILogger<MoMoPaymentService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            var orderId = request.OrderId;
            var orderInfo = request.Description;
            var amount = (long)request.Amount;
            var extraData = request.ExtraData != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.ExtraData))) : "";

            // Tạo raw signature
            var rawSignature = $"accessKey={_config.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={request.NotifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_config.PartnerCode}&redirectUrl={request.ReturnUrl}&requestId={requestId}&requestType=captureWallet";
            
            var signature = ComputeHmacSha256(rawSignature, _config.SecretKey);

            var requestData = new
            {
                partnerCode = _config.PartnerCode,
                partnerName = "Sân Bóng",
                storeId = "SanBong",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = request.ReturnUrl,
                ipnUrl = request.NotifyUrl,
                lang = "vi",
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_config.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("MoMo Response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<MoMoCreateResponse>(responseContent);

            if (result?.ResultCode == 0)
            {
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = requestId,
                    PaymentUrl = result.PayUrl,
                    Message = result.Message,
                    Status = PaymentStatus.Pending,
                    Data = new Dictionary<string, string>
                    {
                        { "orderId", orderId },
                        { "requestId", requestId },
                        { "deeplink", result.Deeplink ?? "" },
                        { "qrCodeUrl", result.QrCodeUrl ?? "" }
                    }
                };
            }

            return new PaymentResult
            {
                Success = false,
                ErrorCode = result?.ResultCode.ToString(),
                Message = result?.Message ?? "Lỗi không xác định",
                Status = PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MoMo payment");
            return new PaymentResult
            {
                Success = false,
                Message = ex.Message,
                Status = PaymentStatus.Failed
            };
        }
    }

    public async Task<PaymentResult> VerifyPaymentAsync(string transactionId, Dictionary<string, string> callbackData)
    {
        try
        {
            // Lấy các tham số từ callback
            var partnerCode = callbackData.GetValueOrDefault("partnerCode", "");
            var orderId = callbackData.GetValueOrDefault("orderId", "");
            var requestId = callbackData.GetValueOrDefault("requestId", "");
            var amount = callbackData.GetValueOrDefault("amount", "0");
            var orderInfo = callbackData.GetValueOrDefault("orderInfo", "");
            var orderType = callbackData.GetValueOrDefault("orderType", "");
            var transId = callbackData.GetValueOrDefault("transId", "");
            var resultCode = callbackData.GetValueOrDefault("resultCode", "-1");
            var message = callbackData.GetValueOrDefault("message", "");
            var payType = callbackData.GetValueOrDefault("payType", "");
            var responseTime = callbackData.GetValueOrDefault("responseTime", "");
            var extraData = callbackData.GetValueOrDefault("extraData", "");
            var signature = callbackData.GetValueOrDefault("signature", "");

            // Tạo raw signature để verify
            var rawSignature = $"accessKey={_config.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            
            var computedSignature = ComputeHmacSha256(rawSignature, _config.SecretKey);

            if (signature != computedSignature)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "Invalid signature",
                    Status = PaymentStatus.Failed
                };
            }

            if (resultCode == "0")
            {
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = transId,
                    Message = message,
                    Status = PaymentStatus.Success,
                    Data = new Dictionary<string, string>
                    {
                        { "orderId", orderId },
                        { "transId", transId },
                        { "amount", amount }
                    }
                };
            }

            return new PaymentResult
            {
                Success = false,
                ErrorCode = resultCode,
                Message = message,
                Status = resultCode == "1006" ? PaymentStatus.Cancelled : PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying MoMo payment");
            return new PaymentResult
            {
                Success = false,
                Message = ex.Message,
                Status = PaymentStatus.Failed
            };
        }
    }

    public async Task<PaymentResult> CheckPaymentStatusAsync(string transactionId)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            var rawSignature = $"accessKey={_config.AccessKey}&orderId={transactionId}&partnerCode={_config.PartnerCode}&requestId={requestId}";
            var signature = ComputeHmacSha256(rawSignature, _config.SecretKey);

            var requestData = new
            {
                partnerCode = _config.PartnerCode,
                requestId = requestId,
                orderId = transactionId,
                lang = "vi",
                signature = signature
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_config.QueryEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MoMoQueryResponse>(responseContent);

            return new PaymentResult
            {
                Success = result?.ResultCode == 0,
                TransactionId = result?.TransId,
                Message = result?.Message,
                Status = result?.ResultCode == 0 ? PaymentStatus.Success : PaymentStatus.Failed,
                Data = new Dictionary<string, string>
                {
                    { "resultCode", result?.ResultCode.ToString() ?? "" },
                    { "amount", result?.Amount.ToString() ?? "" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MoMo payment status");
            return new PaymentResult
            {
                Success = false,
                Message = ex.Message,
                Status = PaymentStatus.Failed
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            var rawSignature = $"accessKey={_config.AccessKey}&amount={(long)amount}&description={reason}&orderId={transactionId}&partnerCode={_config.PartnerCode}&requestId={requestId}&transId={transactionId}";
            var signature = ComputeHmacSha256(rawSignature, _config.SecretKey);

            var requestData = new
            {
                partnerCode = _config.PartnerCode,
                orderId = transactionId,
                requestId = requestId,
                amount = (long)amount,
                transId = transactionId,
                lang = "vi",
                description = reason,
                signature = signature
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_config.RefundEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MoMoRefundResponse>(responseContent);

            return new PaymentResult
            {
                Success = result?.ResultCode == 0,
                TransactionId = result?.TransId,
                Message = result?.Message,
                Status = result?.ResultCode == 0 ? PaymentStatus.Refunded : PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding MoMo payment");
            return new PaymentResult
            {
                Success = false,
                Message = ex.Message,
                Status = PaymentStatus.Failed
            };
        }
    }

    private string ComputeHmacSha256(string rawData, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

// Response classes for MoMo API
public class MoMoCreateResponse
{
    public string? PartnerCode { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public long Amount { get; set; }
    public long ResponseTime { get; set; }
    public string? Message { get; set; }
    public int ResultCode { get; set; }
    public string? PayUrl { get; set; }
    public string? Deeplink { get; set; }
    public string? QrCodeUrl { get; set; }
}

public class MoMoQueryResponse
{
    public string? PartnerCode { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public string? ExtraData { get; set; }
    public long Amount { get; set; }
    public string? TransId { get; set; }
    public string? PayType { get; set; }
    public int ResultCode { get; set; }
    public long RefundTrans { get; set; }
    public string? Message { get; set; }
}

public class MoMoRefundResponse
{
    public string? PartnerCode { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public long Amount { get; set; }
    public string? TransId { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
}

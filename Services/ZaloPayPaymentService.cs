using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SanBong.Services;

/// <summary>
/// Cấu hình ZaloPay
/// </summary>
public class ZaloPayConfig
{
    public int AppId { get; set; }
    public string Key1 { get; set; } = null!;
    public string Key2 { get; set; } = null!;
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/create";
    public string QueryEndpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/query";
    public string RefundEndpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/refund";
}

/// <summary>
/// Service thanh toán ZaloPay
/// Tài liệu: https://docs.zalopay.vn/v2/
/// </summary>
public class ZaloPayPaymentService : IPaymentService
{
    private readonly ZaloPayConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZaloPayPaymentService> _logger;

    public ZaloPayPaymentService(ZaloPayConfig config, HttpClient httpClient, ILogger<ZaloPayPaymentService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appTransId = $"{DateTime.Now:yyMMdd}_{request.OrderId}";
            var embedData = request.ExtraData != null ? JsonSerializer.Serialize(request.ExtraData) : "{}";
            var items = "[]";

            // Tạo MAC
            var data = $"{_config.AppId}|{appTransId}|{request.CustomerName ?? ""}|{(long)request.Amount}|{appTime}|{embedData}|{items}";
            var mac = ComputeHmacSha256(data, _config.Key1);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("app_id", _config.AppId.ToString()),
                new KeyValuePair<string, string>("app_user", request.CustomerName ?? "user"),
                new KeyValuePair<string, string>("app_time", appTime.ToString()),
                new KeyValuePair<string, string>("amount", ((long)request.Amount).ToString()),
                new KeyValuePair<string, string>("app_trans_id", appTransId),
                new KeyValuePair<string, string>("embed_data", embedData),
                new KeyValuePair<string, string>("item", items),
                new KeyValuePair<string, string>("description", request.Description),
                new KeyValuePair<string, string>("bank_code", ""),
                new KeyValuePair<string, string>("callback_url", request.NotifyUrl),
                new KeyValuePair<string, string>("mac", mac)
            });

            var response = await _httpClient.PostAsync(_config.Endpoint, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("ZaloPay Response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<ZaloPayCreateResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.ReturnCode == 1)
            {
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = appTransId,
                    PaymentUrl = result.OrderUrl,
                    Message = result.ReturnMessage,
                    Status = PaymentStatus.Pending,
                    Data = new Dictionary<string, string>
                    {
                        { "appTransId", appTransId },
                        { "zpTransToken", result.ZpTransToken ?? "" }
                    }
                };
            }

            return new PaymentResult
            {
                Success = false,
                ErrorCode = result?.ReturnCode.ToString(),
                Message = result?.ReturnMessage ?? "Lỗi không xác định",
                Status = PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZaloPay payment");
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
            var dataStr = callbackData.GetValueOrDefault("data", "");
            var reqMac = callbackData.GetValueOrDefault("mac", "");

            // Verify MAC
            var computedMac = ComputeHmacSha256(dataStr, _config.Key2);

            if (reqMac != computedMac)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "Invalid MAC",
                    Status = PaymentStatus.Failed
                };
            }

            var callbackDataObj = JsonSerializer.Deserialize<ZaloPayCallbackData>(dataStr);

            return new PaymentResult
            {
                Success = true,
                TransactionId = callbackDataObj?.ZpTransId.ToString(),
                Message = "Thanh toán thành công",
                Status = PaymentStatus.Success,
                Data = new Dictionary<string, string>
                {
                    { "appTransId", callbackDataObj?.AppTransId ?? "" },
                    { "zpTransId", callbackDataObj?.ZpTransId.ToString() ?? "" },
                    { "amount", callbackDataObj?.Amount.ToString() ?? "" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying ZaloPay payment");
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
            var data = $"{_config.AppId}|{transactionId}|{_config.Key1}";
            var mac = ComputeHmacSha256(data, _config.Key1);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("app_id", _config.AppId.ToString()),
                new KeyValuePair<string, string>("app_trans_id", transactionId),
                new KeyValuePair<string, string>("mac", mac)
            });

            var response = await _httpClient.PostAsync(_config.QueryEndpoint, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ZaloPayQueryResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var status = result?.ReturnCode switch
            {
                1 => PaymentStatus.Success,
                2 => PaymentStatus.Failed,
                3 => PaymentStatus.Pending,
                _ => PaymentStatus.Failed
            };

            return new PaymentResult
            {
                Success = result?.ReturnCode == 1,
                TransactionId = result?.ZpTransId.ToString(),
                Message = result?.ReturnMessage,
                Status = status,
                Data = new Dictionary<string, string>
                {
                    { "returnCode", result?.ReturnCode.ToString() ?? "" },
                    { "amount", result?.Amount.ToString() ?? "" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ZaloPay payment status");
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
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var uid = $"{timestamp}{new Random().Next(111, 999)}";
            var mRefundId = $"{DateTime.Now:yyMMdd}_{_config.AppId}_{uid}";

            var data = $"{_config.AppId}|{transactionId}|{(long)amount}|{reason}|{timestamp}";
            var mac = ComputeHmacSha256(data, _config.Key1);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("app_id", _config.AppId.ToString()),
                new KeyValuePair<string, string>("m_refund_id", mRefundId),
                new KeyValuePair<string, string>("zp_trans_id", transactionId),
                new KeyValuePair<string, string>("amount", ((long)amount).ToString()),
                new KeyValuePair<string, string>("description", reason),
                new KeyValuePair<string, string>("timestamp", timestamp.ToString()),
                new KeyValuePair<string, string>("mac", mac)
            });

            var response = await _httpClient.PostAsync(_config.RefundEndpoint, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ZaloPayRefundResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new PaymentResult
            {
                Success = result?.ReturnCode == 1,
                TransactionId = result?.RefundId.ToString(),
                Message = result?.ReturnMessage,
                Status = result?.ReturnCode == 1 ? PaymentStatus.Refunded : PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding ZaloPay payment");
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

// Response classes for ZaloPay API
public class ZaloPayCreateResponse
{
    public int ReturnCode { get; set; }
    public string? ReturnMessage { get; set; }
    public int SubReturnCode { get; set; }
    public string? SubReturnMessage { get; set; }
    public string? ZpTransToken { get; set; }
    public string? OrderUrl { get; set; }
    public string? OrderToken { get; set; }
}

public class ZaloPayQueryResponse
{
    public int ReturnCode { get; set; }
    public string? ReturnMessage { get; set; }
    public int SubReturnCode { get; set; }
    public string? SubReturnMessage { get; set; }
    public bool IsProcessing { get; set; }
    public long Amount { get; set; }
    public long ZpTransId { get; set; }
}

public class ZaloPayRefundResponse
{
    public int ReturnCode { get; set; }
    public string? ReturnMessage { get; set; }
    public int SubReturnCode { get; set; }
    public string? SubReturnMessage { get; set; }
    public long RefundId { get; set; }
}

public class ZaloPayCallbackData
{
    public int AppId { get; set; }
    public string? AppTransId { get; set; }
    public long AppTime { get; set; }
    public string? AppUser { get; set; }
    public long Amount { get; set; }
    public string? EmbedData { get; set; }
    public string? Item { get; set; }
    public long ZpTransId { get; set; }
    public long ServerTime { get; set; }
    public int Channel { get; set; }
    public string? MerchantUserId { get; set; }
    public long UserFeeAmount { get; set; }
    public long DiscountAmount { get; set; }
}

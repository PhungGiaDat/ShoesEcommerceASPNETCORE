using Microsoft.Extensions.Configuration;
using ShoesEcommerce.ViewModels.Payment;
using System.Security.Cryptography;
using System.Text;

namespace ShoesEcommerce.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(IConfiguration config, ILogger<VnPayService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Gets VNPay configuration value from environment variable or appsettings
        /// </summary>
        private string GetConfigValue(string key, string? defaultValue = null)
        {
            // Environment variable names: VNPAY_TMN_CODE, VNPAY_HASH_SECRET, VNPAY_URL, VNPAY_RETURN_URL
            var envKey = $"VNPAY_{key.Replace(":", "_").ToUpper()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            
            if (!string.IsNullOrEmpty(envValue))
                return envValue;
            
            var configValue = _config[$"VNPay:{key}"];
            if (!string.IsNullOrEmpty(configValue))
                return configValue;
            
            return defaultValue ?? string.Empty;
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context)
        {
            var vnp_TmnCode = GetConfigValue("TmnCode");
            var vnp_HashSecret = GetConfigValue("HashSecret");
            var vnp_Url = GetConfigValue("Url", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
            var vnp_ReturnUrl = GetConfigValue("ReturnUrl");

            // Validate required configuration
            if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                _logger.LogError("VNPay configuration is missing. Set VNPAY_TMN_CODE and VNPAY_HASH_SECRET environment variables.");
                throw new InvalidOperationException("VNPay configuration is missing. Please configure TmnCode and HashSecret.");
            }

            var tick = DateTime.Now.Ticks.ToString();

            var vnp_Params = new SortedDictionary<string, string>();
            vnp_Params["vnp_Version"] = "2.1.0";
            vnp_Params["vnp_Command"] = "pay";
            vnp_Params["vnp_TmnCode"] = vnp_TmnCode;
            vnp_Params["vnp_Amount"] = ((long)(amount * 100)).ToString();
            vnp_Params["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss");
            vnp_Params["vnp_CurrCode"] = "VND";
            vnp_Params["vnp_IpAddr"] = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            vnp_Params["vnp_Locale"] = "vn";
            vnp_Params["vnp_OrderInfo"] = $"Thanh toan don hang #{orderId}";
            vnp_Params["vnp_OrderType"] = "other";
            vnp_Params["vnp_ReturnUrl"] = vnp_ReturnUrl;
            vnp_Params["vnp_TxnRef"] = $"{orderId}_{tick}";

            string query = "";
            string hashData = "";

            foreach (var kv in vnp_Params)
            {
                query += $"{kv.Key}={Uri.EscapeDataString(kv.Value)}&";
                hashData += $"{kv.Key}={kv.Value}&";
            }

            query = query.TrimEnd('&');
            hashData = hashData.TrimEnd('&');

            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(vnp_HashSecret));
            var signData = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData));
            var vnp_SecureHash = BitConverter.ToString(signData).Replace("-", "").ToLower();

            _logger.LogInformation("VNPay payment URL created for order {OrderId}", orderId);
            
            return $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";
        }

        public VnPayReturnModel ProcessReturn(IQueryCollection queryParams)
        {
            var vnp_HashSecret = GetConfigValue("HashSecret");
            
            if (string.IsNullOrEmpty(vnp_HashSecret))
            {
                _logger.LogError("VNPay HashSecret is not configured");
                throw new InvalidOperationException("VNPay HashSecret is not configured");
            }
            
            string secureHash = queryParams["vnp_SecureHash"];

            var response = new VnPayReturnModel
            {
                Vnp_TransactionNo = queryParams["vnp_TransactionNo"],
                Vnp_OrderInfo = queryParams["vnp_OrderInfo"],
                Vnp_ResponseCode = queryParams["vnp_ResponseCode"],
                Vnp_TxnRef = queryParams["vnp_TxnRef"],
                Vnp_Amount = queryParams["vnp_Amount"],
                Vnp_SecureHash = secureHash
            };

            var vnp_Params = new SortedDictionary<string, string>();
            foreach (var key in queryParams.Keys)
            {
                if (key != "vnp_SecureHash")
                {
                    vnp_Params.Add(key, queryParams[key].ToString());
                }
            }

            string hashData = "";
            foreach (var kv in vnp_Params)
            {
                hashData += $"{kv.Key}={kv.Value}&";
            }
            hashData = hashData.TrimEnd('&');

            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(vnp_HashSecret));
            var signData = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData));
            var expectedHash = BitConverter.ToString(signData).Replace("-", "").ToLower();

            response.IsSuccess = expectedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("VNPay return processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, IsSuccess={IsSuccess}",
                response.Vnp_TxnRef, response.Vnp_ResponseCode, response.IsSuccess);

            return response;
        }
    }
}
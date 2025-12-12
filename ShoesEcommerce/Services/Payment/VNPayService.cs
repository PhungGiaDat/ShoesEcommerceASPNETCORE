using Microsoft.Extensions.Configuration;
using ShoesEcommerce.ViewModels.Payment;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
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
            var envKey = $"VNPAY_{key.ToUpper()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            
            if (!string.IsNullOrEmpty(envValue))
            {
                _logger.LogDebug("VNPay config '{Key}' loaded from environment variable", key);
                return envValue;
            }
            
            var configValue = _config[$"VNPay:{key}"];
            if (!string.IsNullOrEmpty(configValue))
            {
                _logger.LogDebug("VNPay config '{Key}' loaded from appsettings", key);
                return configValue;
            }
            
            _logger.LogWarning("VNPay config '{Key}' not found, using default: {Default}", key, defaultValue ?? "null");
            return defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Get client IP address, ensuring IPv4 format for VNPay
        /// </summary>
        private string GetIpAddress(HttpContext context)
        {
            try
            {
                var ipAddress = context.Connection.RemoteIpAddress;
                
                if (ipAddress != null)
                {
                    // If IPv6, try to get IPv4
                    if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ipAddress = Dns.GetHostEntry(ipAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    }
                    
                    if (ipAddress != null)
                    {
                        return ipAddress.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting IP address, using fallback");
            }
            
            return "127.0.0.1";
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context)
        {
            var vnp_TmnCode = GetConfigValue("TmnCode");
            var vnp_HashSecret = GetConfigValue("HashSecret");
            var vnp_Url = GetConfigValue("Url", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
            var vnp_ReturnUrl = GetConfigValue("ReturnUrl");

            // Log configuration for debugging
            _logger.LogInformation("VNPay Config - TmnCode: {TmnCode}, Url: {Url}, ReturnUrl: {ReturnUrl}",
                string.IsNullOrEmpty(vnp_TmnCode) ? "NOT SET" : vnp_TmnCode.Substring(0, Math.Min(4, vnp_TmnCode.Length)) + "***",
                vnp_Url,
                vnp_ReturnUrl);

            // Validate required configuration
            if (string.IsNullOrEmpty(vnp_TmnCode))
            {
                _logger.LogError("VNPay TmnCode is missing");
                throw new InvalidOperationException("VNPay TmnCode is not configured. Please set VNPAY_TMNCODE environment variable or VNPay:TmnCode in appsettings.");
            }
            
            if (string.IsNullOrEmpty(vnp_HashSecret))
            {
                _logger.LogError("VNPay HashSecret is missing");
                throw new InvalidOperationException("VNPay HashSecret is not configured. Please set VNPAY_HASHSECRET environment variable or VNPay:HashSecret in appsettings.");
            }

            if (string.IsNullOrEmpty(vnp_ReturnUrl))
            {
                _logger.LogError("VNPay ReturnUrl is missing");
                throw new InvalidOperationException("VNPay ReturnUrl is not configured. Please set VNPAY_RETURNURL environment variable or VNPay:ReturnUrl in appsettings.");
            }

            var tick = DateTime.Now.Ticks.ToString();
            var ipAddr = GetIpAddress(context);
            
            // Convert amount to VND (multiply by 100 as VNPay requires)
            var vnpAmount = ((long)(amount * 100)).ToString();
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var txnRef = $"{orderId}_{tick}";
            var orderInfo = $"Thanh toan don hang {orderId}";

            _logger.LogInformation("Creating VNPay URL - OrderId: {OrderId}, Amount: {Amount} VND, TxnRef: {TxnRef}, IP: {IP}",
                orderId, amount, txnRef, ipAddr);

            // Build query parameters in sorted order
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_Amount", vnpAmount);
            vnp_Params.Add("vnp_CreateDate", createDate);
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_IpAddr", ipAddr);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", orderInfo);
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_ReturnUrl", vnp_ReturnUrl);
            vnp_Params.Add("vnp_TxnRef", txnRef);

            // Build query string and hash data
            var queryBuilder = new StringBuilder();
            var hashDataBuilder = new StringBuilder();

            foreach (var kv in vnp_Params.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                // Query string uses URL encoding
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
                queryBuilder.Append("=");
                queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
                queryBuilder.Append("&");

                // Hash data uses URL encoding too (VNPay requirement)
                hashDataBuilder.Append(WebUtility.UrlEncode(kv.Key));
                hashDataBuilder.Append("=");
                hashDataBuilder.Append(WebUtility.UrlEncode(kv.Value));
                hashDataBuilder.Append("&");
            }

            // Remove trailing &
            var query = queryBuilder.ToString().TrimEnd('&');
            var hashData = hashDataBuilder.ToString().TrimEnd('&');

            // Calculate HMAC SHA512
            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, hashData);

            // Build final URL
            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";

            _logger.LogInformation("VNPay payment URL created successfully for order {OrderId}", orderId);
            _logger.LogDebug("VNPay URL: {Url}", paymentUrl);

            return paymentUrl;
        }

        public VnPayReturnModel ProcessReturn(IQueryCollection queryParams)
        {
            var vnp_HashSecret = GetConfigValue("HashSecret");
            
            if (string.IsNullOrEmpty(vnp_HashSecret))
            {
                _logger.LogError("VNPay HashSecret is not configured");
                throw new InvalidOperationException("VNPay HashSecret is not configured");
            }
            
            var vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();

            _logger.LogInformation("Processing VNPay return - SecureHash: {Hash}", 
                string.IsNullOrEmpty(vnp_SecureHash) ? "MISSING" : vnp_SecureHash.Substring(0, Math.Min(10, vnp_SecureHash.Length)) + "...");

            var response = new VnPayReturnModel
            {
                Vnp_TransactionNo = queryParams["vnp_TransactionNo"],
                Vnp_OrderInfo = queryParams["vnp_OrderInfo"],
                Vnp_ResponseCode = queryParams["vnp_ResponseCode"],
                Vnp_TxnRef = queryParams["vnp_TxnRef"],
                Vnp_Amount = queryParams["vnp_Amount"],
                Vnp_SecureHash = vnp_SecureHash
            };

            // Build hash data from response (excluding vnp_SecureHash and vnp_SecureHashType)
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            foreach (var key in queryParams.Keys)
            {
                var keyLower = key.ToLower();
                if (keyLower != "vnp_securehash" && keyLower != "vnp_securehashtype" && !string.IsNullOrEmpty(queryParams[key]))
                {
                    vnp_Params.Add(key, queryParams[key].ToString());
                }
            }

            var hashDataBuilder = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                hashDataBuilder.Append(WebUtility.UrlEncode(kv.Key));
                hashDataBuilder.Append("=");
                hashDataBuilder.Append(WebUtility.UrlEncode(kv.Value));
                hashDataBuilder.Append("&");
            }
            var hashData = hashDataBuilder.ToString().TrimEnd('&');

            var expectedHash = HmacSHA512(vnp_HashSecret, hashData);
            response.IsSuccess = expectedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
            
            _logger.LogInformation("VNPay return processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, IsSuccess={IsSuccess}",
                response.Vnp_TxnRef, response.Vnp_ResponseCode, response.IsSuccess);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("VNPay hash validation failed. Expected: {Expected}, Got: {Got}",
                    expectedHash.Substring(0, 20) + "...", 
                    vnp_SecureHash?.Substring(0, Math.Min(20, vnp_SecureHash?.Length ?? 0)) + "...");
            }

            return response;
        }

        /// <summary>
        /// Compute HMAC SHA512 hash
        /// </summary>
        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }
            
            return hash.ToString();
        }
    }

    /// <summary>
    /// Comparer for VNPay parameter sorting (ASCII order)
    /// </summary>
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
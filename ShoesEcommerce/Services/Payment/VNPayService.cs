using Microsoft.Extensions.Configuration; // Dùng IConfiguration
using ShoesEcommerce.ViewModels.Payment;
using System.Security.Cryptography;
using System.Text;

namespace ShoesEcommerce.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context)
        {
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_Url = _config["VNPay:Url"];
            var vnp_ReturnUrl = _config["VNPay:ReturnUrl"];

            var tick = DateTime.Now.Ticks.ToString();

            var vnp_Params = new SortedDictionary<string, string>();
            vnp_Params["vnp_Version"] = "2.1.0";
            vnp_Params["vnp_Command"] = "pay";
            vnp_Params["vnp_TmnCode"] = vnp_TmnCode;
            vnp_Params["vnp_Amount"] = ((long)(amount * 100)).ToString(); // 100 = convert to VND
            vnp_Params["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss");
            vnp_Params["vnp_CurrCode"] = "VND";
            vnp_Params["vnp_IpAddr"] = context.Connection.RemoteIpAddress?.ToString();
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

            // Hash
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(vnp_HashSecret));
            var signData = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData));
            var vnp_SecureHash = BitConverter.ToString(signData).Replace("-", "").ToLower();

            return $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";
        }

        public VnPayReturnModel ProcessReturn(IQueryCollection queryParams)
        {
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            string secureHash = queryParams["vnp_SecureHash"];

            // 1. Khởi tạo đối tượng response và gán các giá trị từ queryParams
            var response = new VnPayReturnModel
            {
                Vnp_TransactionNo = queryParams["vnp_TransactionNo"],
                Vnp_OrderInfo = queryParams["vnp_OrderInfo"],
                Vnp_ResponseCode = queryParams["vnp_ResponseCode"],
                Vnp_TxnRef = queryParams["vnp_TxnRef"],
                Vnp_Amount = queryParams["vnp_Amount"],
                Vnp_SecureHash = secureHash
            };

            // 2. Tạo dictionary chứa các tham số trừ vnp_SecureHash
            var vnp_Params = new SortedDictionary<string, string>();
            foreach (var key in queryParams.Keys)
            {
                if (key != "vnp_SecureHash")
                {
                    vnp_Params.Add(key, queryParams[key].ToString());
                }
            }

            // 3. Tạo chuỗi HashData
            string hashData = "";
            foreach (var kv in vnp_Params)
            {
                hashData += $"{kv.Key}={kv.Value}&";
            }
            hashData = hashData.TrimEnd('&');

            // 4. Tính toán Hash mới
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(vnp_HashSecret));
            var signData = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData));
            var expectedHash = BitConverter.ToString(signData).Replace("-", "").ToLower();

            // 5. Kiểm tra Hash và gán IsSuccess
            response.IsSuccess = expectedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);

            return response;
        }
    }
}
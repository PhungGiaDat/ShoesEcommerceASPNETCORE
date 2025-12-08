using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShoesEcommerce.Models.Payments.PayPal;

namespace ShoesEcommerce.Services.Payment
{
    public sealed class PayPalClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _mode;
        private readonly ILogger<PayPalClient> _logger;
        private readonly HttpClient _httpClient;

        public string BaseUrl => _mode == "Live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        public PayPalClient(
            string clientId, 
            string clientSecret, 
            string mode,
            ILogger<PayPalClient> logger,
            IHttpClientFactory httpClientFactory)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _mode = mode ?? "Sandbox";
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClientFactory.CreateClient("PayPal");
            
            _logger.LogInformation("PayPalClient initialized in {Mode} mode", _mode);
        }

        /// <summary>
        /// Authenticate with PayPal to get access token
        /// </summary>
        private async Task<AuthResponse> AuthenticateAsync()
        {
            try
            {
                _logger.LogInformation("Authenticating with PayPal...");

                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                var content = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials")
                };

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{BaseUrl}/v1/oauth2/token"),
                    Method = HttpMethod.Post,
                    Headers =
                    {
                        { "Authorization", $"Basic {auth}" }
                    },
                    Content = new FormUrlEncodedContent(content)
                };

                var httpResponse = await _httpClient.SendAsync(request);
                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("PayPal authentication failed: {StatusCode} - {Response}", 
                        httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"PayPal authentication failed: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);
                
                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal auth response");
                    throw new InvalidOperationException("Failed to deserialize PayPal auth response");
                }

                _logger.LogInformation("PayPal authentication successful");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PayPal authentication");
                throw;
            }
        }

        /// <summary>
        /// Create a PayPal order with amount breakdown including discounts
        /// </summary>
        public async Task<CreateOrderResponse> CreateOrderAsync(
            decimal subtotal,
            decimal discountAmount,
            decimal totalAmount,
            string referenceId,
            string returnUrl,
            string cancelUrl,
            string? description = null)
        {
            try
            {
                _logger.LogInformation(
                    "Creating PayPal order - Subtotal: {Subtotal} VND, Discount: {Discount} VND, Total: {Total} VND, Reference: {Reference}",
                    subtotal, discountAmount, totalAmount, referenceId);

                var auth = await AuthenticateAsync();

                // Convert VND to USD (approximate rate: 1 USD = 24,000 VND)
                const decimal vndToUsdRate = 24000m;
                var subtotalInUsd = Math.Round(subtotal / vndToUsdRate, 2);
                var discountInUsd = discountAmount > 0 ? Math.Round(discountAmount / vndToUsdRate, 2) : 0m;
                var totalInUsd = Math.Round(totalAmount / vndToUsdRate, 2);

                _logger.LogInformation(
                    "Converted to USD - Subtotal: ${SubtotalUsd}, Discount: ${DiscountUsd}, Total: ${TotalUsd}",
                    subtotalInUsd, discountInUsd, totalInUsd);

                // Build amount object - PayPal requires item_total - discount = value
                var amount = new Amount
                {
                    currency_code = "USD",
                    value = totalInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                };

                // Only add breakdown if there's a discount, otherwise PayPal might reject it
                if (discountAmount > 0 && discountInUsd > 0)
                {
                    amount.breakdown = new Breakdown
                    {
                        item_total = new Amount
                        {
                            currency_code = "USD",
                            value = subtotalInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        },
                        discount = new Amount
                        {
                            currency_code = "USD",
                            value = discountInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    };

                    // Verify the math: item_total - discount should equal value
                    var calculatedTotal = subtotalInUsd - discountInUsd;
                    if (Math.Abs(calculatedTotal - totalInUsd) > 0.01m)
                    {
                        _logger.LogWarning(
                            "PayPal amount mismatch - Calculated: ${Calculated}, Expected: ${Expected}. Adjusting breakdown.",
                            calculatedTotal, totalInUsd);
                        
                        // Adjust to match PayPal's expectations
                        amount.breakdown.item_total.value = totalInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        amount.breakdown.discount = null; // Remove discount to avoid mismatch
                    }
                }
                else
                {
                    // No discount - simpler structure
                    amount.breakdown = new Breakdown
                    {
                        item_total = new Amount
                        {
                            currency_code = "USD",
                            value = totalInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    };
                }

                var request = new CreateOrderRequest
                {
                    intent = "CAPTURE",
                    application_context = new ApplicationContext
                    {
                        brand_name = "ShoesEcommerce",
                        landing_page = "BILLING",
                        user_action = "PAY_NOW",
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    },
                    purchase_units = new List<PurchaseUnit>
                    {
                        new()
                        {
                            reference_id = referenceId,
                            description = description ?? "ShoesEcommerce Order",
                            amount = amount
                        }
                    }
                };

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpResponse = await _httpClient.PostAsJsonAsync(
                    $"{BaseUrl}/v2/checkout/orders", 
                    request);

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create PayPal order: {StatusCode} - {Response}",
                        httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to create PayPal order: {httpResponse.StatusCode} - {jsonResponse}");
                }

                var response = JsonSerializer.Deserialize<CreateOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal create order response");
                    throw new InvalidOperationException("Failed to deserialize PayPal create order response");
                }

                _logger.LogInformation("PayPal order created successfully: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                throw;
            }
        }

        /// <summary>
        /// Capture payment for a PayPal order
        /// </summary>
        public async Task<CaptureOrderResponse> CaptureOrderAsync(string orderId)
        {
            try
            {
                _logger.LogInformation("Capturing PayPal order: {OrderId}", orderId);

                var auth = await AuthenticateAsync();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpContent = new StringContent("", Encoding.Default, "application/json");

                var httpResponse = await _httpClient.PostAsync(
                    $"{BaseUrl}/v2/checkout/orders/{orderId}/capture", 
                    httpContent);

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to capture PayPal order {OrderId}: {StatusCode} - {Response}",
                        orderId, httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to capture PayPal order: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<CaptureOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal capture response for order {OrderId}", orderId);
                    throw new InvalidOperationException("Failed to deserialize PayPal capture response");
                }

                _logger.LogInformation("PayPal order captured successfully: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Get order details from PayPal
        /// </summary>
        public async Task<CaptureOrderResponse> GetOrderDetailsAsync(string orderId)
        {
            try
            {
                _logger.LogInformation("Getting PayPal order details: {OrderId}", orderId);

                var auth = await AuthenticateAsync();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpResponse = await _httpClient.GetAsync(
                    $"{BaseUrl}/v2/checkout/orders/{orderId}");

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get PayPal order details {OrderId}: {StatusCode} - {Response}",
                        orderId, httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to get PayPal order details: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<CaptureOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal order details for {OrderId}", orderId);
                    throw new InvalidOperationException("Failed to deserialize PayPal order details");
                }

                _logger.LogInformation("PayPal order details retrieved: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal order details {OrderId}", orderId);
                throw;
            }
        }
    }
}

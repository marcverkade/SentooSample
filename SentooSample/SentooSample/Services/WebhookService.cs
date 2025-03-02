using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SentooSample.Services
{
    public class WebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // This is a secret key that should be stored securely
        private const string SentooSecret = "513e1f99-21a0-4777-b950-6e70481bee2f";
        private const string MerchantId = "3656f346-0e1d-4fc2-bee3-86fe22fb54e1";

        public string ReceivedTransactionId { get; private set; } = "";
        public string WebhookData { get; private set; } = "";
        public string PaymentStatus { get; private set; } = "";

        public event Action? OnChange;

        public WebhookService(IHttpClientFactory httpClientFactory)
        {
            // This is isngleton service, it cannot consume a scoped HTTP client so we have to use a IHttpClientFactory
            _httpClientFactory = httpClientFactory;
        }

        // Update webhook data and fetch payment status
        public async Task UpdateWebhookDataAsync(string transactionId, string data)
        {
            // This is a workaround to make sure the method is async
            await Task.Run(async () =>
            {
                ReceivedTransactionId = transactionId;
                WebhookData = data;
                await CheckPaymentStatus(transactionId);
                OnChange?.Invoke();
            });
        }

        // Check the status of a payment automatically
        private async Task CheckPaymentStatus(string transactionId)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionId))
                {
                    PaymentStatus = "No transaction ID available. Please create a payment first.";
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient();
                var url = $"https://api.sandbox.sentoo.io/v1/payment/status/{MerchantId}/{transactionId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-SENTOO-SECRET", SentooSecret);

                var response = await httpClient.SendAsync(request);
                PaymentStatus = response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync()
                    : $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                PaymentStatus = $"Exception: {ex.Message}";
            }
        }
    }
}

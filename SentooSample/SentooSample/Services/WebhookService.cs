using System;
using System.Threading.Tasks;

namespace SentooSample.Services
{
    public class WebhookService
    {
        public string ReceivedTransactionId { get; private set; } = "";
        public string WebhookData { get; private set; } = "";

        public event Action? OnChange;

        // ✅ Ensure UI updates run on the Dispatcher
        public async Task UpdateWebhookDataAsync(string transactionId, string data)
        {
            await Task.Run(() =>
            {
                ReceivedTransactionId = transactionId;
                WebhookData = data;
                OnChange?.Invoke();
            });
        }
    }
}

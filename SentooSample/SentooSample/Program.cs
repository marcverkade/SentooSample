using Microsoft.AspNetCore.Http;
using System.Web;
using SentooSample.Components;
using SentooSample.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://api.sandbox.sentoo.io") });

// Register WebhookService as a Singleton
builder.Services.AddSingleton<WebhookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SentooSample.Client._Imports).Assembly);

// Add Webhook Endpoint
// Blazor does not support POST requests, so we need to use a workaround
app.MapPost("/checkstatus", async (HttpContext context, WebhookService webhookService) =>
{
    // Create the log file and make sure the folder exists
    var logFile = Path.Combine(AppContext.BaseDirectory, "logs", "webhook_log.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

    try
    {
        await File.AppendAllTextAsync(logFile, $"[{DateTime.UtcNow}] Webhook received\n");

        // Read the data
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        await File.AppendAllTextAsync(logFile, $"[{DateTime.UtcNow}] Webhook body: {requestBody}\n");

        // See if we have data
        if (string.IsNullOrEmpty(requestBody))
        {
            await File.AppendAllTextAsync(logFile, "[Error] Request body is empty.\n");
            return Results.Problem("Request body is empty.", statusCode: 400);
        }

        // Parse the data 
        var form = HttpUtility.ParseQueryString(requestBody);
        var transactionId = form["transaction_id"] ?? "Unknown";

        // Update WebhookService
        await webhookService.UpdateWebhookDataAsync(transactionId, requestBody);

        // Log the transaction ID
        await File.AppendAllTextAsync(logFile, $"Transaction ID: {transactionId}\n");

        return Results.Ok(new { success = true, transaction_id = transactionId });
    }
    catch (Exception ex)
    {
        // An error occurred, log it
        await File.AppendAllTextAsync(logFile, $"Internal Server Error: {ex.Message}\n");
        return Results.Problem($"Error processing webhook request: {ex.Message}", statusCode: 500);
    }
});

// Add the test-webhook endpoint
app.MapGet("/test-webhook", async (WebhookService webhookService) =>
{
    // Create the log file and make sure the folder exists
    var logFile = Path.Combine(AppContext.BaseDirectory, "logs", "webhook_log.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

    // Log the start of the test
    await File.AppendAllTextAsync(logFile, $"Test Webhook Start!\n");

    // Create a fake transaction ID
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"); // Format: YYYYMMDD-HHMMSS
    var fakeTransactionId = $"test-transaction-{timestamp}";
    var fakeRequestBody = $"transaction_id={fakeTransactionId}";

    try
    {
        // Update WebhookService singleton
        await webhookService.UpdateWebhookDataAsync(fakeTransactionId, fakeRequestBody);
    }
    catch (Exception ex)
    {
        // An error occurred, log it
        await File.AppendAllTextAsync(logFile, $"Internal Server Error: {ex.Message}\n");
        return Results.Problem($"Error processing webhook request: {ex.Message}", statusCode: 500);
    }

    // Log the end of the test
    await File.AppendAllTextAsync(logFile, $"Test Webhook Triggered!\n");

    return Results.Ok(new { success = true, transaction_id = fakeTransactionId });
});

app.Run();

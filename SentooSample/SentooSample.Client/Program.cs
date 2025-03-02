using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://api.sandbox.sentoo.io") });

await builder.Build().RunAsync();

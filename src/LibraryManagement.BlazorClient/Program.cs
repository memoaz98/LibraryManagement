using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LibraryManagement.BlazorClient;
using LibraryManagement.BlazorClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException(
        "ApiBaseUrl is missing from wwwroot/appsettings.json.");

builder.Services.AddSingleton<AuthState>();
builder.Services.AddScoped<ITokenStorage, LocalStorageTokenStorage>();
builder.Services.AddScoped<AuthMessageHandler>();
builder.Services.AddScoped<AuthClientService>();

// Typed HttpClient: HttpClient pre-configured with BaseAddress and the
// AuthMessageHandler attached to its pipeline.
builder.Services.AddHttpClient("LibraryApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthMessageHandler>();

// Default HttpClient: returns the named "LibraryApi" instance, so
// @inject HttpClient anywhere uses the authenticated client.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("LibraryApi"));

var host = builder.Build();

var authClient = host.Services.GetRequiredService<AuthClientService>();
await authClient.RestoreFromStorageAsync();

await host.RunAsync();
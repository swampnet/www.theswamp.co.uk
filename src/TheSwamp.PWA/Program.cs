using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;
using TheSwamp.PWA;
using TheSwamp.PWA.Models;
using TheSwamp.PWA.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Derive the site origin so API calls hit /api/wine (not /pwa/api/wine).
var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
var origin = $"{baseUri.Scheme}://{baseUri.Host}{(baseUri.IsDefaultPort ? "" : ":" + baseUri.Port)}/";

// Explicitly fetch the API key from the server-side config endpoint.
// The server injects the key at /pwa/appsettings.json so it never lives
// in a static file. We fetch it once at startup with a temporary HttpClient.
var apiKey = string.Empty;
using (var startupHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
    try
    {
        var config = await startupHttp.GetFromJsonAsync<AppConfig>("appsettings.json");
        apiKey = config?.ApiKey ?? string.Empty;
    }
    catch
    {
        // Falls through with empty key — wine search will return 401 which is surfaced in the UI.
    }
}

var appConfig = new AppConfig { ApiKey = apiKey };

builder.Services.AddSingleton(appConfig);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(origin) });
builder.Services.AddScoped<WineApiService>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();

# PWA — TheSwamp.PWA

A Blazor WebAssembly app hosted inside `TheSwamp.WWW` and installable as a Progressive Web App. Currently provides wine search functionality. Served at `/pwa/`.

---

## Project structure

```
src/
  TheSwamp.PWA/           Blazor WASM project (SDK: Microsoft.NET.Sdk.BlazorWebAssembly)
    Pages/
      Home.razor          Landing page
      WineSearch.razor    Wine search UI (@page "/wine")
    Layout/
      MainLayout.razor    Shell with header + nav bar
    Models/
      AppConfig.cs        Holds ApiKey — populated at startup from /pwa/appsettings.json
      WineDto.cs          Wine search result DTO
    Services/
      WineApiService.cs   Calls GET /api/wine with X-Api-Key header
    wwwroot/
      index.html          Entry point — <base href="/pwa/">
      manifest.webmanifest  PWA metadata (name, icons, display: standalone)
      service-worker.js   Offline caching (Blazor default)
    Program.cs            WASM entry — fetches API key, registers services

  TheSwamp.WWW/           Blazor Server host — serves the WASM bundle
```

`TheSwamp.PWA` is referenced by `TheSwamp.WWW` as a project reference. At build time, the WASM output is merged into the host via Static Web Assets and published under the `/pwa/` path segment (`<StaticWebAssetBasePath>pwa</StaticWebAssetBasePath>`).

---

## How the wiring works (it feels like witchcraft)

You might look at the codebase and wonder: *"Where does `/pwa/` come from? I can't see any route registration for it."* Here's what's actually happening.

### The project reference does most of the work automatically

When `TheSwamp.WWW.csproj` has this:

```xml
<ProjectReference Include="..\TheSwamp.PWA\TheSwamp.PWA.csproj" />
```

...the .NET SDK's **Static Web Assets** system activates. At build time it says: *"this referenced project is a Blazor WASM app — I'll pull all of its `wwwroot/` files and compiled output (the .NET WASM runtime, DLLs, etc.) into the host app's static files."*

The `StaticWebAssetBasePath` property in `TheSwamp.PWA.csproj` controls where they land:

```xml
<StaticWebAssetBasePath>pwa</StaticWebAssetBasePath>
```

Everything from the WASM project gets mounted under `/pwa/`. This is handled entirely by the SDK — no manual route registration, no copying files, no config. `app.MapStaticAssets()` in `Program.cs` then serves them like any other static file.

### What you do need to add manually

Just two lines in `TheSwamp.WWW/Program.cs`, for two things the SDK can't know about:

```csharp
// 1. Client-side routing support.
//    The WASM app handles its own routes (/pwa/wine, etc.) in the browser.
//    But if someone navigates to /pwa/wine directly, the server has no
//    matching route and would return 404. This catches any unknown path
//    under /pwa/ and returns index.html — Blazor then handles the route client-side.
app.MapFallbackToFile("/pwa/{*path:nonfile}", "/pwa/index.html");

// 2. Dynamic config injection (see "API key injection" section below).
//    There's no physical appsettings.json file in the WASM bundle.
//    This is a server-side endpoint that returns the API key at runtime.
app.MapGet("/pwa/appsettings.json", (IConfiguration config) =>
    Results.Json(new { ApiKey = config["PWA:ApiKey"] }));
```

### Summary

| What | Who does it |
|---|---|
| Compiling the WASM app to WebAssembly | `TheSwamp.PWA` project build |
| Mounting WASM output files under `/pwa/` | SDK Static Web Assets (automatic from `<ProjectReference>`) |
| Serving those files over HTTP | `app.MapStaticAssets()` in `TheSwamp.WWW` |
| Making `/pwa/wine` not 404 on direct navigation | `MapFallbackToFile` — added manually |
| Injecting the API key | `MapGet("/pwa/appsettings.json")` — added manually |

The project reference handles ~95% of the wiring. The two manual lines exist only because of the routing edge case and the secret key injection.

---

## Architecture overview

```
┌─────────────────────────────────────────────────────┐
│                    Browser                          │
│                                                     │
│  ┌─────────────────────────────────────────────┐    │
│  │  Blazor WASM (TheSwamp.PWA)                 │    │
│  │  running in WebAssembly runtime             │    │
│  │                                             │    │
│  │  Pages: Home, WineSearch                    │    │
│  │  Services: WineApiService                   │    │
│  │  AppConfig { ApiKey }                       │    │
│  └──────────────┬──────────────────────────────┘    │
│                 │  HTTP (same origin)               │
└─────────────────┼───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│           TheSwamp.WWW  (Blazor Server)             │
│                                                     │
│  GET /pwa/appsettings.json  ──► injects API key     │
│  GET /api/wine?term=…       ──► WineController      │
│  GET /pwa/*                 ──► serves WASM bundle  │
│  GET /pwa/{*nonfile}        ──► /pwa/index.html     │
│                                                     │
│  ApiKeyMiddleware validates X-Api-Key on /api/*     │
│  IApiKeyService: cache → DB lookup                  │
└─────────────────────────────────────────────────────┘
```

The WASM app runs entirely in the browser after the initial download. All API calls go back to the same origin (`theswamp.co.uk`), so there are no CORS concerns.

---

## Static asset hosting

The `StaticWebAssetBasePath` property in `TheSwamp.PWA.csproj` pins all WASM static files under `/pwa/`:

```xml
<StaticWebAssetBasePath>pwa</StaticWebAssetBasePath>
```

Without this, the WASM project's Bootstrap and other assets would collide with the host app's assets at the root path. The result is:

| Resource | URL |
|---|---|
| WASM entry point | `/pwa/index.html` |
| .NET WASM runtime | `/pwa/_framework/*.wasm` |
| App CSS | `/pwa/TheSwamp.PWA.styles.css` |
| PWA manifest | `/pwa/manifest.webmanifest` |
| Service worker | `/pwa/service-worker.js` |

`index.html` carries `<base href="/pwa/">` so all relative URLs inside the WASM app resolve under `/pwa/`.

---

## Request flow — wine search

```
Browser (WASM)                  TheSwamp.WWW
      │                               │
      │  GET /api/wine?term=shiraz    │
      │  X-Api-Key: <raw key>         │
      │ ─────────────────────────────►│
      │                               │
      │                    ApiKeyMiddleware
      │                    ├─ extract X-Api-Key header
      │                    ├─ SHA-256 hash the raw key
      │                    ├─ check ApiKeyCache (memory)
      │                    │   └─ on miss: query DB
      │                    ├─ verify user has "api" role
      │                    └─ attach ClaimsPrincipal to request
      │                               │
      │                        WineController
      │                        └─ query wine DB
      │                               │
      │  200 OK  [ WineDto[] ]        │
      │ ◄─────────────────────────────│
      │                               │
  WineSearch.razor renders results
```

---

## API key injection

The WASM app needs an API key to authenticate calls to `/api/*`. The key must not be committed to source control or shipped as a static file inside the WASM bundle (it would be trivially readable by anyone who installs the PWA).

### How it works

`TheSwamp.WWW` serves a **dynamic** `appsettings.json` response at the WASM's base URL:

```csharp
// TheSwamp.WWW/Program.cs
app.MapGet("/pwa/appsettings.json", (IConfiguration config) =>
    Results.Json(new { ApiKey = config["PWA:ApiKey"] ?? string.Empty }));
```

On startup, `TheSwamp.PWA/Program.cs` fetches this endpoint explicitly before the DI container is built:

```csharp
// TheSwamp.PWA/Program.cs
using var startupHttp = new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)  // https://theswamp.co.uk/pwa/
};

var config = await startupHttp.GetFromJsonAsync<AppConfig>("appsettings.json");
// resolves to GET /pwa/appsettings.json — handled by the MapGet above

var appConfig = new AppConfig { ApiKey = config?.ApiKey ?? string.Empty };
builder.Services.AddSingleton(appConfig);
```

`WineApiService` receives `AppConfig` via DI and attaches the key to every request:

```csharp
request.Headers.Add("X-Api-Key", _config.ApiKey);
```

### Sequence diagram

```
Browser (WASM startup)          TheSwamp.WWW
      │                               │
      │  GET /pwa/appsettings.json    │
      │ ─────────────────────────────►│
      │                               │  reads PWA:ApiKey
      │                               │  from server appsettings.json
      │  200 { "apiKey": "abc123" }   │  (never a static file)
      │ ◄─────────────────────────────│
      │                               │
  AppConfig.ApiKey = "abc123"
  registered as singleton in DI
      │
  WineApiService injected
  with AppConfig
      │
      │  (user searches)
      │
      │  GET /api/wine?term=shiraz
      │  X-Api-Key: abc123
      │ ─────────────────────────────►│
```

### Why not embed the key at build/publish time?

Baking the key into the WASM bundle (e.g. via a transform at publish time) would work but has downsides:

- Rotating the key requires a redeployment
- The key is frozen into the service worker cache and won't update until the user's browser picks up a new service worker

The dynamic fetch approach means the key can be changed in `appsettings.json` and takes effect on the next browser startup without redeployment.

### Configuration

Add the raw API key (as shown once at generation time — not the hash) to `appsettings.json` on the server:

```json
{
  "PWA": {
    "ApiKey": "<raw key>"
  }
}
```

The key must belong to a user with the `"api"` role in the Identity database. `ApiKeyMiddleware` validates the key on every `/api/*` request.

---

## PWA installation

When a user visits `/pwa/` in a supporting browser (Chrome, Edge), the browser detects `manifest.webmanifest` and the registered service worker and offers an "Install" prompt. Once installed:

- The app runs in a standalone window (no browser chrome)
- The service worker caches the WASM runtime and app shell for offline use
- On next launch, the WASM loads from cache; the `appsettings.json` fetch still goes to the server (requires connectivity to get a fresh key)

### Navigating to the PWA

A link to `/pwa/` is present on the main site home page. Deep links (e.g. `/pwa/wine`) are handled by:

```csharp
app.MapFallbackToFile("/pwa/{*path:nonfile}", "/pwa/index.html");
```

This catches any path under `/pwa/` that doesn't match a physical file and returns the WASM shell, which then handles client-side routing.

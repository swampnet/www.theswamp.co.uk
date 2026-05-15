# www.theswamp.co.uk

Personal website for pj. Blazor Server (.NET 10), SQL Server, SignalR chat, OIDC auth.

---

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio)
- An account on Azure Portal, Google Cloud Console, and/or GitHub (for OIDC)

---

## First-time setup

### 1. Configure secrets

Copy `src/Theswamp.WWW/appsettings.example.json` to `src/Theswamp.WWW/appsettings.json`
and fill in the values:

| Key | Where to get it |
|---|---|
| `ConnectionStrings:DefaultConnection` | LocalDB is the default; no change needed for dev |
| `Authentication:Microsoft:*` | [Azure Portal → App registrations](https://portal.azure.com) |
| `Authentication:Google:*` | [Google Cloud Console → Credentials](https://console.cloud.google.com) |
| `Authentication:GitHub:*` | [GitHub → Developer settings → OAuth Apps](https://github.com/settings/developers) |

> `appsettings.json` is gitignored and will never be committed.

### 2. Register redirect URIs with OIDC providers

For local dev (`https://localhost:<port>`), register these callback URLs:

- **Microsoft**: `https://localhost:<port>/signin-microsoft`
- **Google**: `https://localhost:<port>/signin-google`
- **GitHub**: `https://localhost:<port>/signin-github`

### 3. Run via Aspire (recommended)

```
cd src/Theswamp.Orchestration
dotnet run
```

This launches the Aspire dashboard and the Blazor app. The DB will be
migrated and roles seeded automatically on first run.

### 4. Assign the first admin user

After registering/logging in for the first time, run this SQL against the database:

```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'your@email.com'
  AND r.Name  = 'admin';
```

Once you have one admin, use the `/admin` page to manage further users.

---

## API usage

All `/api/*` endpoints require the `X-Api-Key` header:

```
X-Api-Key: <value from ApiSettings:ApiKey>
```

| Method | URL | Description |
|---|---|---|
| GET | `/api/wine?term` | Search LWIN data |
| GET | `/api/messages` | Last 50 chat messages |
| POST | `/api/messages` | Post a message (broadcasts via SignalR) |

POST body:
```json
{ "text": "Hello world" }
```

---

## Problems encountered

### Blazor WASM PWA fails to load at `/pwa/` after pulling to a new machine

**Symptom:** Browser console shows a 404 for a URL like:
```
/pwa/_content/Microsoft.DotNet.HotReload.WebAssembly.Browser/Microsoft.DotNet.HotReload.WebAssembly.Browser.*.lib.module.js
```

**Cause:** `Microsoft.AspNetCore.Components.WebAssembly` 10.0.8 added a hot reload library
initializer (an RCL static asset) to `Microsoft.DotNet.HotReload.WebAssembly.Browser`.
The WASM dotnet runtime requests it relative to the document base URL (`/pwa/`), so it asks
for `/pwa/_content/...`. But RCL static assets are always registered at the host root
(`/_content/...`), so the file exists but at the wrong path.

**Fix:** Add a `MapGet` endpoint in `TheSwamp.WWW/Program.cs` that redirects
`/pwa/_content/**` to `/_content/**`:

```csharp
app.MapGet("/pwa/_content/{**path}", (string path) =>
    Results.Redirect("/_content/" + path));
```

### `/pwa/` returns 404 in Azure (production) but works locally

**Symptom:** Browsing to `/pwa/` or `/pwa/index.html` returns a plain 404 on the deployed app.

**Cause:** `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` in `TheSwamp.PWA.csproj` causes `index.html` to be embedded in the DLL (for Blazor's import-map injection pipeline) rather than published as a static file. `MapFallbackToFile` can't find it, so every request to `/pwa/` 404s. The dev build serves it via the source-linked static web asset manifest, which is why it works locally.

**Fix:** Remove `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` from `TheSwamp.PWA.csproj` (this property causes `index.html` to be embedded in the DLL rather than published as a static file). Also update `index.html` to use the non-fingerprinted script path `_framework/blazor.webassembly.js` — without the property, the `#[.{fingerprint}]` placeholder is never substituted so the browser requests a literally-named file that doesn't exist. The non-fingerprinted route is served by `MapStaticAssets()` with `Cache-Control: no-cache`.

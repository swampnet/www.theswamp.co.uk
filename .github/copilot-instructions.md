# Copilot Instructions — www.theswamp.co.uk

Personal website for pj. Blazor Server (.NET 10), SQL Server (LocalDB in dev), SignalR chat, OIDC auth via ASP.NET Core Identity. Includes a Blazor WASM PWA (`TheSwamp.PWA`) hosted within `TheSwamp.WWW` and served at `/pwa/`.

---

## Build & Run

```bash
# Run via Aspire (recommended — launches dashboard + web app)
cd src/TheSwamp.Orchestration
dotnet run

# Build the web project directly
cd src/TheSwamp.WWW
dotnet build

# EF migrations (run from TheSwamp.WWW)
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

No automated tests exist yet.

---

## Architecture

### Two projects

| Project | Role |
|---|---|
| `src/TheSwamp.Orchestration` | .NET Aspire AppHost — launches and orchestrates `TheSwamp.WWW` |
| `src/TheSwamp.WWW` | Blazor Server web app — all application code lives here |
| `src/TheSwamp.PWA` | Blazor WASM PWA — hosted inside `TheSwamp.WWW`, served at `/pwa/` |

Aspire project reference uses underscores: `Projects.TheSwamp_WWW` (dots → underscores, Aspire SDK convention).

### TheSwamp.WWW layout

```
Api/            MVC API controllers — /api/* routes
Components/
  Pages/        Blazor pages (.razor)
  Layout/       NavMenu, MainLayout
  Account/      Scaffolded Identity pages
Data/           DbContext, ApplicationUser, EF Migrations
Hubs/           SignalR ChatHub
Middleware/     ApiKeyMiddleware
Models/         EF entity models + DTOs
Services/       IChatService, IConnectionTracker, RoleSeeder
```

### Request flow

- **Blazor pages** → call services directly (server-side, no HTTP round-trip)
- **API controllers** (`/api/*`) → protected by `ApiKeyMiddleware` (`X-Api-Key` header) → call services
- **SignalR hub** (`/hubs/chat`) → calls `IChatService` for all message sends so DB + broadcast are always consistent
- **`IChatService`** is Scoped (matches `ApplicationDbContext`); **`IConnectionTracker`** is Singleton (shared across hub instances)

### Chat architecture

All message sends go through `IChatService.SendMessageAsync(string? userId, string text)`:
- Persists to `ChatMessage` table with `UserId` (nullable FK to `AspNetUsers.Id`) — never a raw username
- Resolves display name at send time via `UserManager<ApplicationUser>`; broadcasts resolved name to SignalR clients
- `ConnectionTracker` (singleton `ConcurrentDictionary`) tracks active SignalR connections; admin panel in `Chat.razor` reads from it live
- `GetRecentMessagesAsync` returns messages from the **last 24 hours** (up to the requested count) — not purely count-based
- API-posted messages (`POST /api/messages`) always use `null` for `userId`, so they display as `"Anon"` in chat

### Authentication

- Cookie-based via ASP.NET Core Identity
- External OIDC: Microsoft Entra ID is the only active provider; Google and GitHub are wired up but commented out in `Program.cs`
- Roles `"admin"` and `"api"` are seeded by `RoleSeeder` on startup
- First admin must be assigned manually via SQL (see README); subsequent admins via the `/admin` page
- API endpoints use a separate `X-Api-Key` header (not cookies)
- Users must have the `"api"` role to generate/use an API key; removing the role immediately invalidates the cache

### Database

- `ApplicationDbContext` extends `IdentityDbContext<ApplicationUser, IdentityRole, string>`
- `ChatMessage` table (mapped via `.ToTable("ChatMessage")` in `OnModelCreating`) — `Id` (bigint), `UserId` (nullable, MaxLength 450), `Text`, `SentOnUtc`
- `DbSet` property is named `ChatMessages` but the underlying table is `ChatMessage` — don't confuse the two
- Migrations run automatically on startup via `db.Database.MigrateAsync()`
- `PhoneNumber` / `PhoneNumberConfirmed` are intentionally excluded from `ApplicationUser` via `.Ignore()` in `OnModelCreating`

### ApplicationUser custom properties

`ApplicationUser` extends `IdentityUser` with two extra columns:

| Property | Type | Purpose |
|---|---|---|
| `DisplayName` | `string?` (MaxLength 100) | User-chosen display name; takes precedence over `UserName` and `Email` in all display contexts |
| `ApiKeyHash` | `string?` (MaxLength 64) | SHA-256 hex of the active API key; `null` means no key |

Display name resolution order (used by `ChatService.GetDisplayNameAsync`): `DisplayName` → `UserName` → `Email` → `"Anon"`

---

## Key Conventions

### C# style

- `var` for all local variables
- File-scoped namespaces (`namespace Foo.Bar;`)
- Tabs for indentation
- Allman brace style (opening brace on its own line)
- Always use curly braces, even for single-line `if`/`for`/`foreach`
- Comment anything non-obvious; avoid noise comments

### Blazor pages

- All interactive pages need `@rendermode InteractiveServer`
- Guard SignalR / JS-dependent setup with `if (!RendererInfo.IsInteractive) return;` in `OnInitializedAsync` — pre-render will fire first without JS
- Hub URL must be absolute: use `NavigationManager.ToAbsoluteUri(...)` 
- Pages call services directly — no internal HTTP API calls from Blazor components

### JavaScript

- Plain JS / jQuery only — no TypeScript, no frontend frameworks
- K&R brace style (opening brace on same line)
- Tabs for indentation

### SignalR hub events (client-side names)

| Event | Args |
|---|---|
| `ReceiveMessage` | `(displayName: string, text: string, sentAt: string)` |
| `UserConnected` | `(displayName: string)` |
| `UserDisconnected` | `(displayName: string)` |

### API

- All `/api/*` routes require `X-Api-Key: <value>` header
- The key owner must have the `"api"` role — requests from users without it return 401
- Swagger UI is available at `/swagger` in all environments; spec at `/swagger/v1/swagger.json`
- Keys are **per-user**, stored as SHA-256 hashes in `AspNetUsers.ApiKeyHash` (never the raw key)
- `ApiKeyCache` (singleton) — maps `hash → CacheEntry(userId, hasApiRole)` with a reverse `userId → hash` map; evict by hash on revoke/regenerate, by userId when the `"api"` role is removed
- `IApiKeyService` (scoped) — `GenerateAsync`, `RevokeAsync`, `ValidateAsync(rawKey)` — checks cache first, falls back to DB; role check is always cache-free after the first validation
- Middleware resolves `IApiKeyService` from `context.RequestServices` (avoids scoped-in-singleton lifetime issue)
- Users manage their key at `/Account/Manage` (generate → raw key shown once; revoke)

### Secrets / config

- `appsettings.json` is gitignored — never commit it
- `appsettings.example.json` has placeholder values and IS committed
- Keys to configure: `ConnectionStrings:DefaultConnection`, `Authentication:Microsoft:*`, `Authentication:Google:*`, `Authentication:GitHub:*`
- PWA API key: `PWA:ApiKey` — must be the **raw key** (not the hash) for a user with the `"api"` role

### EF migrations — manual schema changes

If you alter the DB schema outside EF (e.g. rename a table or column directly in SQL Server), edit the migration file that would make that change to remove the now-redundant step and use `IF EXISTS` guards or dynamic SQL where constraint names may vary. Do **not** use `dotnet ef database drop` — it drops the entire database, not just EF tables.

---

## PWA (TheSwamp.PWA)

`TheSwamp.PWA` is a Blazor WebAssembly project referenced by `TheSwamp.WWW`. The SDK's Static Web Assets system automatically mounts all WASM output under `/pwa/` at build time — no manual route registration needed for the files themselves.

### How the hosting works

- `<StaticWebAssetBasePath>pwa</StaticWebAssetBasePath>` in `TheSwamp.PWA.csproj` controls the mount path
- `app.MapStaticAssets()` in `TheSwamp.WWW/Program.cs` serves them (automatic — no extra config)
- Two lines **must** be added manually to `TheSwamp.WWW/Program.cs`:

```csharp
// Client-side routing — return index.html for unknown paths so Blazor handles them
app.MapFallbackToFile("/pwa/{*path:nonfile}", "/pwa/index.html");

// Dynamic config — injects API key; there is no physical appsettings.json in the bundle
app.MapGet("/pwa/appsettings.json", (IConfiguration config) =>
    Results.Json(new { ApiKey = config["PWA:ApiKey"] ?? string.Empty }));
```

### API key injection

The PWA needs an API key to call `/api/*`. The key is **never** embedded in the WASM bundle. Instead:

1. `TheSwamp.WWW` serves it dynamically at `/pwa/appsettings.json` (reads `PWA:ApiKey` from server config)
2. `TheSwamp.PWA/Program.cs` fetches that endpoint explicitly at startup using a temporary `HttpClient` before the DI container is built
3. The resolved key is registered as a singleton `AppConfig` and injected into `WineApiService`
4. `WineApiService` sends `X-Api-Key: <key>` on every `/api/wine` request

The fetch is explicit (not relying on `WebAssemblyHostBuilder.CreateDefault` implicit config loading) so it is visible in browser DevTools and fails loudly if the endpoint is misconfigured.

### PWA pages

| Page | Route | Notes |
|---|---|---|
| `Home.razor` | `/pwa/` | Landing page with links |
| `WineSearch.razor` | `/pwa/wine` | Wine search — calls `WineApiService` |

### Blazor WASM conventions

- `HttpClient.BaseAddress` must be the **site origin** (`https://theswamp.co.uk/`), not the WASM base (`/pwa/`), so `api/wine` resolves to `/api/wine` not `/pwa/api/wine`
- `index.html` has `<base href="/pwa/">` — required for WASM routing and asset resolution
- WASM pages do **not** use `@rendermode` — that's Blazor Server only
- See `documents/pwa.md` for full architecture diagrams and implementation detail

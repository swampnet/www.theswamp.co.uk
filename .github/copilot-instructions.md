# Copilot Instructions — www.theswamp.co.uk

Personal website for pj. Blazor Server (.NET 10), SQL Server (LocalDB in dev), SignalR chat, OIDC auth via ASP.NET Core Identity.

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

### Authentication

- Cookie-based via ASP.NET Core Identity
- External OIDC: Microsoft Entra ID, Google, GitHub — configured in `appsettings.json`
- Role `"admin"` is seeded by `RoleSeeder` on startup
- First admin must be assigned manually via SQL (see README); subsequent admins via the `/admin` page
- API endpoints use a separate `X-Api-Key` header (not cookies)

### Database

- `ApplicationDbContext` extends `IdentityDbContext<ApplicationUser, IdentityRole, string>`
- `ChatMessage` table (mapped via `.ToTable("ChatMessage")` in `OnModelCreating`) — `Id` (bigint), `UserId` (nullable, MaxLength 450), `Text`, `SentOnUtc`
- `DbSet` property is named `ChatMessages` but the underlying table is `ChatMessage` — don't confuse the two
- Migrations run automatically on startup via `db.Database.MigrateAsync()`

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

- All `/api/*` routes require `X-Api-Key: <value>` header (configured in `ApiSettings:ApiKey`)
- Missing or wrong key → 401; key not configured → 503 (fail-safe)

### Secrets / config

- `appsettings.json` is gitignored — never commit it
- `appsettings.example.json` has placeholder values and IS committed
- Keys to configure: `ConnectionStrings:DefaultConnection`, `ApiSettings:ApiKey`, `Authentication:Microsoft:*`, `Authentication:Google:*`, `Authentication:GitHub:*`

### EF migrations — manual schema changes

If you alter the DB schema outside EF (e.g. rename a table or column directly in SQL Server), edit the migration file that would make that change to remove the now-redundant step and use `IF EXISTS` guards or dynamic SQL where constraint names may vary. Do **not** use `dotnet ef database drop` — it drops the entire database, not just EF tables.

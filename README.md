# www.theswamp.co.uk

Personal website for pj. Blazor Server (.NET 10), SQL Server, SignalR chat, OIDC auth.

---

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio)
- An account on Azure Portal, Google Cloud Console, and GitHub (for OIDC)

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

After registering/logging in for the first time, run this SQL against the
`TheswampWWW` LocalDB database:

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
| GET | `/api/forecast` | Example — 5-day weather forecast |
| GET | `/api/messages` | Last 50 chat messages |
| POST | `/api/messages` | Post a message (broadcasts via SignalR) |

POST body:
```json
{ "text": "Hello world" }
```

---

## Project structure

```
src/
  Theswamp.Orchestration/   .NET Aspire AppHost
  Theswamp.WWW/
    Api/                    API controllers (/api/*)
    Components/
      Pages/                Blazor pages
      Layout/               NavMenu, MainLayout
      Account/              Identity scaffolded pages
    Data/                   DbContext, ApplicationUser, Migrations
    Hubs/                   SignalR ChatHub
    Middleware/             ApiKeyMiddleware
    Models/                 ChatMessage entity
    Services/               RoleSeeder
```

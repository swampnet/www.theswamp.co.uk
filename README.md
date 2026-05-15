# www.theswamp.co.uk

Personal website for pj. 

- Blazor Server for main site.
- Blazor WASM for PWA.
- SQL Server, 
- SignalR, 
- OIDC auth.

---

## First-time setup

### Configure secrets

Copy `src/Theswamp.WWW/appsettings.example.json` to `src/Theswamp.WWW/appsettings.json`
and fill in the values:

| Key | Where to get it |
|---|---|
| `ConnectionStrings:DefaultConnection` | LocalDB is the default; no change needed for dev |
| `Authentication:Microsoft:*` | [Azure Portal → App registrations](https://portal.azure.com) |
| `Authentication:Google:*` | [Google Cloud Console → Credentials](https://console.cloud.google.com) |
| `Authentication:GitHub:*` | [GitHub → Developer settings → OAuth Apps](https://github.com/settings/developers) |


### Register redirect URIs with OIDC providers

For local dev (`https://localhost:<port>`), register these callback URLs:

- **Microsoft**: `https://localhost:<port>/signin-microsoft`
- **Google**: `https://localhost:<port>/signin-google`
- **GitHub**: `https://localhost:<port>/signin-github`

### Run via Aspire (recommended)

```
cd src/Theswamp.Orchestration
dotnet run
```

This launches the Aspire dashboard and the Blazor app. The DB will be
migrated and roles seeded automatically on first run.

### Assign the first admin user

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
X-Api-Key: <API-KEY>
```

| Method | URL | Description |
|---|---|---|
| GET | `/api/wine?<term>` | Search LWIN data |
| GET | `/api/messages` | Last 50 chat messages |
| POST | `/api/messages` | Post a message |

POST body:
```json
{ "text": "Hello world" }
```

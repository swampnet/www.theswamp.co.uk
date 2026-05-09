# www.theswamp.co.uk
## Personal website for pj

- c# / .Net
- Use Aspire for develpment orchestration
- Solution file in @C:\dev\www.theswamp.co.uk\
- Projects in \src\ directory
- Doocumentation in \documentation\ directory
- Heavily comment anything slightly complicated
- Target database will be sql-server, but use localDB for development
- Put secrets in appsettings.json, but exclude from git. Add  appsettings.example.json with placeholder values.

# Projects
## Theswamp.Orchestration
Aspire orchestraction

## Theswamp.WWW
A blazor-server web app
This is a personal web page, it should be clean but minimal. We will work on the presentation later, for now just keep it functional and basic

SignalR hub (for chat messages initially. Will expand later)

Most pages are public, use OIDC (Entra, Google, Github) for authentication

### API

Api calls under an /api/ route
- Regular API endpoints
    - Initialy I just need an example endpoint (/api/forecast is fine) and a Get/Post Message endpoint that will broadcast chat messages (post) and list recent messages (get)
- Endpoints must be protected - Can use a simple api-key or something for now. Suggest recomondations

### Pages

- Home page: Just contains links to the other pages.
- Chat: A simple chat page where chat messages posted to the api will appear, and also allows the user to post chat messages. If user is authenticated, use their name else use 'Anon'. Announce in the chat when users connect / disconnect.
- Test: Only when authenticated, display role and claim info for the authenticated user
- Admin: Only for authenticated users with an 'admin' role. User and role management. Create roles, assign/remove roles from users.

---

# TODO

[X] pass user id (or null) to chat post. Use username if given, else 'Anon': Do not pass username from client, get it on the server based on user id. 
    This is more secure and prevents impersonation.
[X] If user is admin, show a list of all connected users & their connection ids + ip address in the chat page.
[ ] MFA setup
[X] Rename: Theswamp -> TheSwamp
[ ] Add ApiKey to user table, and use that for api authentication instead of the api-key in the config. Might want to consider caching the api keys in memory for performance, and invalidate the cache when api keys are updated.
[ ] Add a page for users to manage their api key (generate new, revoke etc)
[ ] Create test project and add some unit tests for the api endpoints and orchestration logic
[ ] Add logging and error handling to the api endpoints

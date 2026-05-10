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
[X] Add ApiKey to user table, and use that for api authentication instead of the api-key in the config. Might want to consider caching the api keys in memory for performance, and invalidate the cache when api keys are updated.
[X] Add a page for users to manage their api key (generate new, revoke etc)
[ ] Create test project and add some unit tests for the api endpoints and orchestration logic
[X] Add logging and error handling. Structured logging - I'm used to Serilog so use that if we can integrate it with all the other stuff (Azure, Aspire). 
    Log to console for now, but make it easy to add other sinks later (file, seq etc)
[X] Deploy from GitHub
[ ] Duplicate current www.theswamp.co.uk home page
[ ] Add wine search
[ ] Add wine review AI
[X] Consolidate Account/Manage, Test, Profile and Api-Key pages under a single page
[X] Add api docs
[X] Drop Phone Number from user / account / everything
[X] Move navigation menu items from the left side of the page to the top, and make it a bit nicer. Maybe use a hamburger menu on mobile. 
    The main home page should have a hero banner that takes up the full width of the page, with a welcome message.
[X] Display build version

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Theswamp.WWW.Components;
using Theswamp.WWW.Components.Account;
using Theswamp.WWW.Data;
using Theswamp.WWW.Hubs;
using Theswamp.WWW.Middleware;
using Theswamp.WWW.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Razor components + Interactive Server rendering (Blazor Server mode)
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// ---------------------------------------------------------------------------
// Authentication — Identity + OIDC providers
// ---------------------------------------------------------------------------
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
	{
		options.DefaultScheme = IdentityConstants.ApplicationScheme;
		options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
	})
	.AddIdentityCookies();

// External OIDC providers — must be chained from AddAuthentication() separately.
builder.Services.AddAuthentication()
	// Microsoft Entra ID (Azure AD) — register an app at portal.azure.com
	.AddMicrosoftAccount(options =>
	{
		options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
		options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
	})
	// Google — register at console.cloud.google.com
	.AddGoogle(options =>
	{
		options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
		options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
	})
	// GitHub — register at github.com/settings/developers
	.AddGitHub(options =>
	{
		options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
		options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
	});

// ---------------------------------------------------------------------------
// Database — SQL Server (LocalDB in development)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------------------------------------------------------------------------
// Identity — with full Role support
// ---------------------------------------------------------------------------
builder.Services.AddIdentityCore<ApplicationUser>(options =>
	{
		options.SignIn.RequireConfirmedAccount = true;
		options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
	})
	.AddRoles<IdentityRole>()  // enables role management
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddSignInManager()
	.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// ---------------------------------------------------------------------------
// SignalR — real-time chat hub
// ---------------------------------------------------------------------------
builder.Services.AddSignalR();

// ---------------------------------------------------------------------------
// API controllers (for /api/* routes)
// ---------------------------------------------------------------------------
builder.Services.AddControllers();

// ---------------------------------------------------------------------------
// Build the app
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Seed roles and run any pending migrations on startup
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	// Apply any pending EF Core migrations automatically on startup.
	// In production you may prefer to run migrations as a deployment step instead.
	await db.Database.MigrateAsync();

	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	await RoleSeeder.SeedRolesAsync(roleManager);
}

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// API key middleware — must run before controller routing so all /api routes are protected.
app.UseMiddleware<ApiKeyMiddleware>();

app.MapStaticAssets();

// SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

// API controllers
app.MapControllers();

// Blazor components
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

// Identity endpoints (Account pages)
app.MapAdditionalIdentityEndpoints();

app.Run();

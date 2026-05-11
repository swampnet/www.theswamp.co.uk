using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using TheSwamp.WWW.Components;
using TheSwamp.WWW.Components.Account;
using TheSwamp.WWW.Data;
using TheSwamp.WWW.Hubs;
using TheSwamp.WWW.Middleware;
using TheSwamp.WWW.Services;

// Bootstrap logger captures any startup errors before the full Serilog config is ready.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

    var builder = WebApplication.CreateBuilder(args);

    // ---------------------------------------------------------------------------
    // Logging — Serilog
    // ---------------------------------------------------------------------------
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();

        // When running under Aspire, the OTLP endpoint env var is set automatically.
        // Wire up the OpenTelemetry sink so logs appear in the Aspire dashboard.
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            configuration.WriteTo.OpenTelemetry(opts =>
            {
                opts.Endpoint = otlpEndpoint.TrimEnd('/') + "/v1/logs";
                opts.Protocol = OtlpProtocol.HttpProtobuf;

                // Parse "key=value,key=value" headers injected by Aspire.
                var headersRaw = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
                if (!string.IsNullOrWhiteSpace(headersRaw))
                {
                    foreach (var pair in headersRaw.Split(','))
                    {
                        var parts = pair.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            opts.Headers[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                opts.ResourceAttributes["service.name"] =
                    Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "TheSwamp-www";
            });
        }
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Trust all proxies — Azure App Service sits behind Microsoft's load balancer
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

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
        //// Google — register at console.cloud.google.com
        //.AddGoogle(options =>
        //{
        //    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        //    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        //})
        //// GitHub — register at github.com/settings/developers
        //.AddGitHub(options =>
        //{
        //    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        //    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        //})
        ;

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
    // Application services
    // ---------------------------------------------------------------------------
    // ChatService is scoped to match ApplicationDbContext's lifetime.
    builder.Services.AddScoped<IChatService, ChatService>();

    // ConnectionTracker is singleton — one shared registry across all hub instances.
    builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();

    // ApiKeyCache is singleton — shared in-memory lookup table of hashed keys.
    builder.Services.AddSingleton<ApiKeyCache>();

    // ApiKeyService is scoped — uses UserManager which depends on ApplicationDbContext.
    builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

    // WineSearchService is scoped to match ApplicationDbContext's lifetime.
    builder.Services.AddScoped<IWineService, WineService>();

    // AIService
    builder.Services.AddTransient<IAIService, AIService>();

    // ---------------------------------------------------------------------------
    // API controllers (for /api/* routes)
    // ---------------------------------------------------------------------------
    builder.Services.AddControllers();

    // ---------------------------------------------------------------------------
    // Swagger / OpenAPI — spec generation + UI (dev only, served at /swagger)
    // ---------------------------------------------------------------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TheSwamp API",
            Version = "v1",
            Description = "API endpoints for TheSwamp. All requests require an `X-Api-Key` header."
        });

        // Declare the API key security scheme.
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Description = "Your personal API key. Generate one from your Account page."
        });

        // Apply the API key requirement globally — v10 uses a delegate so the scheme reference
        // can be resolved against the document being generated.
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
        });

        // Only include routes under /api/
        options.DocInclusionPredicate((_, api) =>
            api.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true);

        // Include XML doc comments from controller <summary> tags.
        options.IncludeXmlComments(System.Reflection.Assembly.GetExecutingAssembly());
    });

    // ---------------------------------------------------------------------------
    // Build the app
    // ---------------------------------------------------------------------------
    var app = builder.Build();

    // ---------------------------------------------------------------------------
    // Seed roles and run any pending migrations on startup
    // ---------------------------------------------------------------------------
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Log.Information("Applying pending database migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedRolesAsync(roleManager, logger);
    }

    // ---------------------------------------------------------------------------
    // HTTP pipeline
    // ---------------------------------------------------------------------------
    app.UseForwardedHeaders();

    // Structured HTTP request logging — one log event per request instead of multiple.
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    // Swagger UI at /swagger — spec at /swagger/v1/swagger.json
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TheSwamp API v1");
        options.RoutePrefix = "swagger";
    });

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

}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

var builder = DistributedApplication.CreateBuilder(args);

// Register the Blazor Server web app as a project resource.
// Aspire will launch it automatically and surface its logs/traces in the dashboard.
builder.AddProject<Projects.TheSwamp_WWW>("TheSwamp-www");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Register the Blazor Server web app as a project resource.
// Aspire will launch it automatically and surface its logs/traces in the dashboard.
builder.AddProject<Projects.Theswamp_WWW>("theswamp-www");

builder.Build().Run();

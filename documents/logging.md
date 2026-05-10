# Logging Configuration

Serilog is the logging provider. Configuration lives in the `Serilog` section of `appsettings.json`.  
The `Logging` (Microsoft default) section is no longer used.

## How it works

| Layer | What happens |
|---|---|
| **Bootstrap logger** | Writes to console from process start, before `appsettings.json` is loaded. Catches startup crashes. |
| **Full logger** | Built from `appsettings.json` via `ReadFrom.Configuration()`. Active for the lifetime of the app. |
| **OTLP sink** | Added automatically at startup when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (Aspire injects this). |
| **HTTP requests** | `UseSerilogRequestLogging()` emits one structured event per request instead of the noisy default ASP.NET Core events. |

---

## Adding sinks

Install the relevant NuGet package in `TheSwamp.WWW`, then add an entry to the `WriteTo` array in `appsettings.json`.  
No code changes needed.

### Seq (local / self-hosted)

```bash
dotnet add package Serilog.Sinks.Seq
```

```json
"WriteTo": [
  { "Name": "Console", "Args": { "outputTemplate": "..." } },
  {
    "Name": "Seq",
    "Args": { "serverUrl": "http://localhost:5341" }
  }
]
```

### Rolling file

```bash
dotnet add package Serilog.Sinks.File
```

```json
{
  "Name": "File",
  "Args": {
    "path": "logs/log-.txt",
    "rollingInterval": "Day",
    "retainedFileCountLimit": 14
  }
}
```

---

## Azure — Application Insights

Application Insights is the recommended sink for Azure App Service.  
It gives structured log search, live metrics, alerting, and correlation with HTTP requests and exceptions.

### 1. Create an Application Insights resource

In the Azure Portal (or via CLI):

```bash
az monitor app-insights component create \
  --app theswamp-insights \
  --location uksouth \
  --resource-group <your-rg> \
  --application-type web
```

Copy the **Connection String** (not the Instrumentation Key — the connection string is preferred).

### 2. Install the sink

```bash
cd src/TheSwamp.WWW
dotnet add package Serilog.Sinks.ApplicationInsights
```

### 3. Add the connection string to configuration

In Azure App Service → Configuration → Application settings, add:

| Name | Value |
|---|---|
| `ApplicationInsights__ConnectionString` | `InstrumentationKey=...;IngestionEndpoint=...` |

For local development, add it to `appsettings.json` (gitignored):

```json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
}
```

### 4. Configure the sink in appsettings

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.ApplicationInsights", "Serilog.Enrichers.Environment" ],
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
      }
    },
    {
      "Name": "ApplicationInsights",
      "Args": {
        "connectionString": "<your-connection-string>",
        "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
      }
    }
  ],
  "Enrich": [ "FromLogContext", "WithMachineName", "WithEnvironmentName" ]
}
```

> **Tip:** Instead of hardcoding the connection string in the sink args, read it from configuration. One way is to configure the sink in code in `Program.cs`:
>
> ```csharp
> .WriteTo.ApplicationInsights(
>     builder.Configuration["ApplicationInsights:ConnectionString"],
>     TelemetryConverter.Traces)
> ```
>
> This keeps secrets out of `appsettings.json` entirely and lets you supply the value via App Service environment variables.

### 5. App Service log streaming

The **Console** sink is always active. Azure App Service can stream console output directly in the portal:

- **App Service → Monitoring → Log stream** — live tail of stdout/stderr
- No extra configuration required; works out of the box with the existing Console sink

---

## Azure — minimum level per environment

Use App Service **Application settings** to override the minimum log level in production without redeploying:

| App setting name | Value |
|---|---|
| `Serilog__MinimumLevel__Default` | `Information` |
| `Serilog__MinimumLevel__Override__Microsoft` | `Warning` |

Azure translates `__` (double underscore) to `:` in configuration keys, so these map directly onto the `Serilog:MinimumLevel:Default` configuration path.

---

## Aspire dashboard (development)

When running via `TheSwamp.Orchestration` (`dotnet run` from the Aspire project), the OTLP endpoint is injected automatically as `OTEL_EXPORTER_OTLP_ENDPOINT`. The app detects this at startup and adds the OpenTelemetry sink, so all structured logs appear in the Aspire dashboard under the **TheSwamp-www** service without any extra configuration.

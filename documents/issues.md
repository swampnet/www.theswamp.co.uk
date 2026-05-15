## Problems encountered

### Blazor WASM PWA fails to load at `/pwa/` after pulling to a new machine

**Symptom:** Browser console shows a 404 for a URL like:
```
/pwa/_content/Microsoft.DotNet.HotReload.WebAssembly.Browser/Microsoft.DotNet.HotReload.WebAssembly.Browser.*.lib.module.js
```

**Cause:** `Microsoft.AspNetCore.Components.WebAssembly` 10.0.8 added a hot reload library
initializer (an RCL static asset) to `Microsoft.DotNet.HotReload.WebAssembly.Browser`.
The WASM dotnet runtime requests it relative to the document base URL (`/pwa/`), so it asks
for `/pwa/_content/...`. But RCL static assets are always registered at the host root
(`/_content/...`), so the file exists but at the wrong path.

**Fix:** Add a `MapGet` endpoint in `TheSwamp.WWW/Program.cs` that redirects
`/pwa/_content/**` to `/_content/**`:

```csharp
app.MapGet("/pwa/_content/{**path}", (string path) =>
    Results.Redirect("/_content/" + path));
```

### `/pwa/` returns 404 in Azure (production) but works locally

**Symptom:** Browsing to `/pwa/` or `/pwa/index.html` returns a plain 404 on the deployed app.

**Cause:** `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` in `TheSwamp.PWA.csproj` causes `index.html` to be embedded in the DLL (for Blazor's import-map injection pipeline) rather than published as a static file. `MapFallbackToFile` can't find it, so every request to `/pwa/` 404s. The dev build serves it via the source-linked static web asset manifest, which is why it works locally.

**Fix:** Remove `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` from `TheSwamp.PWA.csproj` (this property causes `index.html` to be embedded in the DLL rather than published as a static file). Also update `index.html` to use the non-fingerprinted script path `_framework/blazor.webassembly.js` — without the property, the `#[.{fingerprint}]` placeholder is never substituted so the browser requests a literally-named file that doesn't exist. The non-fingerprinted route is served by `MapStaticAssets()` with `Cache-Control: no-cache`.

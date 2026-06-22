---
title: "Cloudflare Pages"
---


Cloudflare Pages hosting **attaches to a JavaScript app that Aspire already models** — it is not a standalone "point at a folder" resource. You add your frontend with the standard Aspire JavaScript APIs, then mark it for Pages.

## Deploy a frontend to Pages

```csharp
builder.AddViteApp("web", "../web")        // or AddNodeApp / AddJavaScriptApp
    .PublishAsCloudflarePages();
```

- **`aspire run`** → the app runs as its normal dev server (e.g. Vite).
- **`aspire deploy`** → the app's build runs, then the output is uploaded with `wrangler pages deploy`. The Pages project is created if it doesn't exist.

## Options

```csharp
builder.AddViteApp("web", "../web")
    .PublishAsCloudflarePages(options =>
    {
        options.ProjectName = "my-site";     // defaults to a sanitized resource name
        options.OutputDirectory = "dist";    // build output dir (Vite default)
        options.Branch = "main";             // production branch
        options.BuildCommand = "npm run build";
        options.SkipBuild = false;           // set true if the output is already built
    });
```

## Talking to your API

Inject the API's URL so the frontend can call it:

```csharp
var api = builder.AddProject<Projects.Api>("api").PublishAsCloudflareContainer();

builder.AddViteApp("web", "../web")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .PublishAsCloudflarePages();
```

In the frontend, read `import.meta.env.VITE_API_URL` and `fetch` from it.

## Requirements

- **wrangler** installed — see [How wrangler authenticates](../../getting-started/wrangler/).
- API token with *Cloudflare Pages: Edit* — see [API Tokens](../../getting-started/api-tokens/).

## Custom domains

```csharp
var zone = builder.AddDeploymentParameter("zone-id");
var host = builder.AddDeploymentParameter("web-hostname");

builder.AddViteApp("web", "../web")
    .PublishAsCloudflarePages()
    .WithCustomDomain(host, zone);
```

Custom domains require *DNS: Edit* and *Zone: Read* on the relevant zone, and the [Zone ID](../../getting-started/account-and-zone-ids/).

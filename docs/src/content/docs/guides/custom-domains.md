---
title: "Custom Domains"
---


Attach a custom hostname to a Container or Pages app with `WithCustomDomain`. Call it **after** `PublishAsCloudflareContainer` / `PublishAsCloudflarePages`.

The Zone ID is account-specific configuration, so pass it as an Aspire **parameter** rather than hard-coding it:

```csharp
var zone = builder.AddDeploymentParameter("zone-id"); // resolved from config / user-secrets / env

var api = builder.AddProject<Projects.Api>("api")
    .PublishAsCloudflareContainer()
    .WithCustomDomain("api.example.com", zone);

builder.AddViteApp("web", "../web")
    .PublishAsCloudflarePages()
    .WithCustomDomain("www.example.com", zone);
```

A literal overload — `WithCustomDomain("api.example.com", "<zone-id>")` — also exists for quick samples, but prefer the parameter for real deployments.

:::tip[Deployment parameters]
`AddDeploymentParameter` (from the **`AvantiPoint.Aspire.Hosting.Extensions`** package) is a parameter that's **only required when you publish/deploy** — during `aspire run` it resolves to an empty/default value, so a Zone ID you don't have locally never blocks your inner loop. At `aspire deploy` it behaves like a normal required `AddParameter`.

```csharp
using AvantiPoint.Aspire.Hosting.Extensions;

var zone = builder.AddDeploymentParameter("zone-id");
```
:::

`WithCustomDomain` is repeatable — call it multiple times to attach multiple hostnames.

## What happens at deploy

| Resource | Behavior |
| --- | --- |
| **Container / Worker** | A [Workers custom domain](https://developers.cloudflare.com/workers/configuration/routing/custom-domains/) is attached. Cloudflare creates the DNS record and issues the SSL certificate automatically. |
| **Pages** | The hostname is added to the Pages project and a proxied `CNAME` to `<project>.pages.dev` is upserted in the zone. |

## Requirements

- The [**Zone ID**](../getting-started/account-and-zone-ids.md) for the domain.
- API token permissions **DNS: Edit** and **Zone: Read** on that zone — see [API Tokens](../getting-started/api-tokens.md). The integration automatically requires these scopes once you use `WithCustomDomain`.

:::note
The domain (zone) must already exist in your Cloudflare account. `WithCustomDomain` configures records within that zone; it does not register domains.
:::

---
title: "Deployment Parameters"
description: "AddDeploymentParameter — parameters required at deploy but optional in local development."
---


`AvantiPoint.Aspire.Hosting.Extensions` provides **`AddDeploymentParameter`**, a small, provider-agnostic helper for configuration that should be **required when you deploy** but **absent during local development** — things like a hostname, a Zone ID, a production connection string, or a token you only need against the real cloud.

A normal `builder.AddParameter("x")` prompts (or fails) when the value is missing, which gets in the way of a credential-free `aspire run`. `AddDeploymentParameter` resolves to a real Aspire parameter during `aspire publish`/`aspire deploy`, but to an empty, non-prompting parameter during `aspire run`.

## Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Required at deploy, optional in dev.
var zone    = builder.AddDeploymentParameter("zone-id");
var apiHost = builder.AddDeploymentParameter("api-hostname");
var dbConn  = builder.AddDeploymentParameter("pg-production-connection", secret: true);
```

These can be passed anywhere a parameter is accepted — for example a Cloudflare custom domain or a Hyperdrive production connection string:

```csharp
api.WithCustomDomain(apiHost, zone);
builder.AddPostgres("pg").PublishAsHyperdrive("hd", dbConn);
```

## Behavior

| Mode | Behavior |
| --- | --- |
| `aspire run` | Resolves to an empty parameter — no prompt, no failure, so the inner loop stays credential-free. |
| `aspire publish` / `aspire deploy` | Resolves to a real Aspire parameter, sourced from environment variables / user-secrets / configuration like any other. |

Pass `secret: true` for sensitive values, and the overload taking a `ParameterDefault` to supply a default.

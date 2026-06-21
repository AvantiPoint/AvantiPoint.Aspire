---
title: "Prerequisites"
---


## A Cloudflare account

You'll need a [Cloudflare account](https://dash.cloudflare.com/sign-up). For **custom domains**, you also need a domain (zone) added to that account.

## Tooling

| Tool | Needed for | Install |
| --- | --- | --- |
| **.NET 10 SDK** | Everything | <https://dotnet.microsoft.com/download> |
| **Aspire CLI** | `aspire run` / `aspire deploy` | `dotnet tool install -g aspire.cli` — see [aspire.dev](https://aspire.dev) |
| **Docker** | The local MinIO R2 emulator during `aspire run`, and building container images | <https://docs.docker.com/get-docker/> |
| **wrangler** | Deploying Pages and Containers (`aspire deploy`) | `npm install -g wrangler` |
| **Node.js** | Building/running JavaScript apps for Pages | <https://nodejs.org> |

:::note
There's no SDK *workload* to install. The AppHost is a normal .NET project that uses the `Aspire.AppHost.Sdk` MSBuild SDK (pulled in by the `Aspire.Hosting.AppHost` package), and the **Aspire CLI** drives `aspire run`/`aspire deploy`.
:::

:::tip
For a **local R2-only dev loop** you don't need wrangler, a Cloudflare token, or even an account — just Docker. The emulator handles everything until you `aspire deploy`.
:::


## Install the packages

Add the hosting packages your app needs to your **AppHost** project:

```bash
dotnet add package AvantiPoint.Aspire.Hosting.Cloudflare
dotnet add package AvantiPoint.Aspire.Hosting.Cloudflare.R2
dotnet add package AvantiPoint.Aspire.Hosting.Cloudflare.Pages
```

And the **client** package in any service that talks to R2:

```bash
dotnet add package AvantiPoint.Aspire.Cloudflare.R2
```

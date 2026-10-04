---
title: "Prerequisites"
---


## A Cloudflare account

You'll need a [Cloudflare account](https://dash.cloudflare.com/sign-up). For **custom domains**, you also need a domain (zone) added to that account.

## Tooling

| Tool | Needed for | Install |
| --- | --- | --- |
| **An SDK supporting .NET 10 targets** | Building the hosting integrations and .NET services | <https://dotnet.microsoft.com/download> |
| **Aspire 13.6 CLI** | C# and TypeScript AppHosts, `aspire run` / `aspire deploy` | `dotnet tool install -g aspire.cli --version 13.6.0` - see [aspire.dev](https://aspire.dev) |
| **Docker** | The local MinIO R2 emulator during `aspire run`, and building container images | <https://docs.docker.com/get-docker/> |
| **wrangler** | Deploying Pages and Containers (`aspire deploy`) | `npm install -g wrangler` |
| **Node.js** | TypeScript AppHosts and JavaScript apps for Pages; Node 24 is tested by the sample | <https://nodejs.org> |

:::note
There's no SDK *workload* to install. C# AppHosts use the `Aspire.AppHost.Sdk` MSBuild SDK. TypeScript AppHosts use the CLI-generated SDK connected to the same .NET hosting integrations. The **Aspire CLI** drives `aspire run`/`aspire deploy` for either language.
:::

:::tip
For a **local R2-only dev loop** you don't need wrangler, a Cloudflare token, or even an account — just Docker. The emulator handles everything until you `aspire deploy`.
:::


## Install the packages

Follow the [quickstart](../quickstart/) package setup for your AppHost language. C# uses project package references; TypeScript lists packages in `aspire.config.json` and runs `aspire restore`. Client packages belong in the consuming .NET service in either case.

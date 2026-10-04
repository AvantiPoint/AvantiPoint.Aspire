# Cloudflare TypeScript AppHost

This sample uses the official Aspire 13.6.0 TypeScript AppHost toolchain. The CLI generates `.aspire/modules/aspire.mts` from the same .NET hosting integrations used by C# AppHosts; these are not JavaScript client libraries or a replacement orchestration implementation.

Install the Aspire 13.6 CLI and Node.js, then run these commands in this directory:

```sh
npm ci
aspire restore
npm run check
aspire run
```

The regular sample runs D1, KV, Queues and Vectorize emulators without Cloudflare credentials or Docker. The config references local `.csproj` files for development; an installed consumer should replace those paths with its AvantiPoint NuGet package versions. Keep `sdk.version` aligned with the Aspire dependencies used by those packages. The generated SDK and CLI-generated `tsconfig.apphost.json` are not committed.

## APIs and overloads

The builder exposes `addCloudflareEnvironment`, `addCloudflareWorker`, `addCloudflareAI`, `addD1Database`, `addKvNamespace`, `addQueue`, `addR2Bucket`, `addVectorizeIndex`, and `addDeploymentParameter`. Environment-scoped overloads use an `InEnvironment` suffix, for example `cloudflare.addD1DatabaseInEnvironment('database')`.

Container and Pages publishing use `publishAsCloudflareContainer` / `publishAsCloudflarePages`, or their `InEnvironment` variants. The generated APIs accept a `configure` option with an async callback. Options properties use async accessors:

```ts
await app.publishAsCloudflarePagesInEnvironment(cloudflare, {
    configure: async options => {
        await options.outputDirectory.set('dist');
        await options.branch.set('main');
    }
});
```

Custom domains have separate `withCustomDomain`, `withCustomDomainParameters`, `withCustomDomainHostnameParameter`, and `withCustomDomainZoneParameter` methods. Hyperdrive has `publishAsHyperdrive`, `publishAsHyperdriveWithParameter`, and `publishAsHyperdriveWithConnectionSource`. The C# overloads retain their existing names and behavior.

The `ParameterDefault` overload of `AddDeploymentParameter` remains a C# API; TypeScript can use the exported no-default overload and ordinary Aspire parameter APIs. These hosting exports do not expose the .NET service-client registration APIs to JavaScript applications. Hosting a TypeScript frontend from a C# AppHost remains supported through `Aspire.Hosting.JavaScript` and requires no TypeScript AppHost.

## Model-only interop validation

`validate.mts` exercises resource identity, builder/environment overloads, references, Vectorize enums, PostgreSQL/Hyperdrive and Worker/Pages/AI/Container options callbacks across the real TypeScript-to-.NET connection. It also evaluates an environment callback through Aspire's execution-configuration builder, without starting that resource. It exits before building, starting, provisioning or deploying the application model. Set a process-local flag when invoking the CLI:

```sh
CLOUDFLARE_INTEROP_TEST=1 aspire run --non-interactive
```

In PowerShell:

```powershell
$env:CLOUDFLARE_INTEROP_TEST = '1'
aspire run --non-interactive
Remove-Item Env:CLOUDFLARE_INTEROP_TEST
```

The assertions require neither cloud credentials nor Docker. Aspire records the `TypeScript interop passed` message in its CLI log; if the CLI remains at `Connecting to AppHost` after that message, press Ctrl+C to stop it. The .NET test suite also checks capability-id uniqueness, exported resource types and the background dispatch needed by synchronous C# configuration callbacks that re-enter the remote host.

Official references: [Aspire 13.6 release](https://github.com/microsoft/aspire/releases/tag/v13.6.0), [TypeScript AppHost example](https://github.com/microsoft/aspire/tree/v13.6.0/playground/TypeScriptAppHost), [export attribute](https://github.com/microsoft/aspire/blob/v13.6.0/src/Aspire.Hosting/Ats/AspireExportAttribute.cs), and [remote-host protocol](https://github.com/microsoft/aspire/blob/v13.6.0/src/Aspire.Hosting.RemoteHost/README.md).

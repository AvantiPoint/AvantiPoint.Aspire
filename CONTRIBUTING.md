# Contributing

Start with an issue for substantial API or architecture changes. Include the affected package, behavior and a small reproducible example. Never include credentials or generated publish artifacts.

## Build and test

Use an SDK that supports the solution's `net10.0` targets and an Aspire 13.6 CLI. `global.json` deliberately does not pin an SDK version. Node.js is needed for JavaScript builds and the Pages subprocess regression test.

```bash
dotnet build AvantiPoint.Aspire.slnx -c Release
dotnet test --project tests/AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Tests/AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Tests.csproj -c Release
```

The repository uses native Microsoft Testing Platform, so use `--project` or `--solution`, not a positional solution argument. The unit-test project allowlist in `.github/workflows/ci.yml` excludes `IntegrationTests`. Run those projects individually for a credential-free unit suite.

```bash
dotnet test --solution AvantiPoint.Aspire.slnx -c Release
```

The solution-wide command also includes integration tests. Playground end-to-end tests need Docker and Node/npm/wrangler and may start containers; they do not require a Cloudflare token. MinIO is used only for local R2 emulation and is omitted in publish/deploy mode. Its community image is pinned by digest; the test checks image availability before starting the AppHost. Real-cloud tests validate token activeness, R2 CRUD, and the actual publish/deploy/destroy steps for an isolated Worker with an R2 binding, including HTTP content and cleanup checks. These tests require Cloudflare credentials and create/delete randomly named `ap-aspire-it-*` resources; they do not deploy the playground or modify production routes/domains. Review their implementation before opting in; do not use a production account. Missing prerequisites cause individual gated tests to skip, not the whole integration project.

For docs changes, run `npm ci`, `npm run check` and `npm run build` in `docs`. Keep public examples complete with package setup and imports.

## Pull requests

Explain the observable change, related issues and validation actually performed. Preserve existing C# consumers. Keep one new type per file, use the existing `.slnx` solution and shared build templates, and do not add SDK pins, NuGet configuration files or bespoke build wrappers. Pull requests build unsigned; trusted pushes to master retain signing through the shared template.

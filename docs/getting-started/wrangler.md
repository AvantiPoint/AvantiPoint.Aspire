# How wrangler authenticates

The integration shells out to [**wrangler**](https://developers.cloudflare.com/workers/wrangler/) — Cloudflare's CLI — to deploy **Pages** and **Containers**. (R2 provisioning and custom-domain DNS use the REST API directly and don't need wrangler.)

## Non-interactive auth

You do **not** run `wrangler login`. The integration passes your credentials to wrangler as environment variables, which is the supported way to authenticate in CI and automation:

| Variable | Value |
| --- | --- |
| `CLOUDFLARE_API_TOKEN` | Your [scoped API token](api-tokens.md). |
| `CLOUDFLARE_ACCOUNT_ID` | Your [account ID](account-and-zone-ids.md). |

These are the same values the rest of the integration uses, so setting them once is enough. When you run `aspire deploy`, the integration sets them on the wrangler process for you.

## Install wrangler

```bash
npm install -g wrangler
wrangler --version
```

The integration calls whatever `wrangler` is on your `PATH`. If wrangler is missing when a Pages/Container deploy runs, you'll get a clear error pointing you here.

> [!NOTE]
> wrangler is only needed for `aspire deploy` of Pages/Containers. Local `aspire run` and R2-only workflows don't use it.

## Token scopes for wrangler

The token needs the permissions for whatever wrangler deploys:

- **Pages:** *Cloudflare Pages: Edit*
- **Workers / Containers:** *Workers Scripts: Edit*

These are the same rows from [API Tokens & Scopes](api-tokens.md) — one token covers both the integration's REST calls and wrangler.

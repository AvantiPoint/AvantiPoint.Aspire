---
title: "API Tokens & Scopes"
---


The integration (and `wrangler`) authenticate to Cloudflare with an **API token**. This is the single most important piece to get right: the token must carry the permissions for the services you deploy — no more, no less.

:::note
This API token is **not** the same as the R2 S3 credentials your app uses at runtime. Those are separate — see [R2 S3 Credentials](../r2-credentials/).
:::


## Create a token

1. Go to **[My Profile → API Tokens](https://dash.cloudflare.com/profile/api-tokens)** (a user token), or **Manage Account → API Tokens** for an [account-owned token](https://developers.cloudflare.com/fundamentals/api/get-started/account-owned-tokens/) (recommended for CI/shared use, not tied to one person).
2. Select **Create Token → Create Custom Token**.
3. Add the **permissions** for the services you'll deploy (see the table below).
4. Set **Account Resources** to your account, and **Zone Resources** to the specific zone(s) only if you use custom domains.
5. Create the token and **copy it now** — Cloudflare shows the secret only once.

Provide it to the integration via the `CLOUDFLARE_API_TOKEN` environment variable (or an Aspire parameter — see [Quickstart](../quickstart/)).

## Which permissions do I need?

The integration only requires the permissions for the resources actually in your AppHost — it validates the token against exactly that set before deploying. Grant the union of the rows that apply to you:

| If you deploy… | Permission group | Level | Category |
| --- | --- | --- | --- |
| **Anything** (always) | User Details | Read | User |
| **R2 buckets** (`…Hosting.Cloudflare.R2`) | Workers R2 Storage | Edit | Account |
| **Containers / .NET APIs** (`…Hosting.Cloudflare`) | Workers Scripts | Edit | Account |
| **Pages** (`…Hosting.Cloudflare.Pages`) | Cloudflare Pages | Edit | Account |
| **Custom domains** (`WithCustomDomain`) | DNS | Edit | Zone |
| **Custom domains** (`WithCustomDomain`) | Zone | Read | Zone |

:::note
Cloudflare groups permissions into **Account**, **Zone**, and **User** categories, each granted at **Edit** or **Read**. Permission-group *names* are cosmetic and can change in the dashboard; if a name differs, search for the closest match in the right category. The authoritative, current list is the [API token permissions reference](https://developers.cloudflare.com/fundamentals/api/reference/permissions/).
:::


## Minimal tokens by scenario

- **R2 only:** *User Details: Read* + *Workers R2 Storage: Edit*.
- **API container + R2:** add *Workers Scripts: Edit*.
- **Full stack (API + Pages + R2):** add *Cloudflare Pages: Edit*.
- **+ custom domains:** add *DNS: Edit* and *Zone: Read*, scoped to the relevant zone(s).

## How validation works

On `aspire deploy`, the first pipeline step calls Cloudflare's [token verify](https://developers.cloudflare.com/fundamentals/api/how-to/test-token/) endpoint. If the token is missing, inactive, or rejected, the deploy stops immediately with a message listing the scopes the operation expected — so you can fix the token before anything is provisioned.

## Security tips

- Prefer **account-owned tokens** for CI so access doesn't depend on one person's account.
- Scope **Zone Resources** to specific zones rather than *All zones* when you can.
- Store the token as a **secret** (GitHub Actions secret, user-secrets) — never commit it.
- Rotate tokens periodically; you can roll or delete them from the same dashboard page.

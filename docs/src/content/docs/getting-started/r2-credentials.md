---
title: "R2 S3 Credentials"
---


R2 has **two** kinds of credentials, and it's important not to confuse them:

| Credential | Used by | Purpose |
| --- | --- | --- |
| **API token** (Bearer) | The integration + `wrangler` | *Provisioning* — creating buckets, deploying. See [API Tokens](api-tokens.md). |
| **R2 S3 credentials** (Access Key ID + Secret Access Key) | Your application at runtime | *Data access* — reading/writing objects over the S3-compatible API. |

This page covers the second kind.

## When you need them

- **Local dev (`aspire run`):** **Never.** The MinIO emulator supplies its own local credentials automatically.
- **Real R2 (`aspire deploy`, or `.RunAsReal()`):** Your app needs S3 credentials to read/write objects. Provide them via the `R2_ACCESS_KEY_ID` and `R2_SECRET_ACCESS_KEY` environment variables (or Aspire parameters).

## Create R2 S3 credentials

1. In the dashboard, open **R2 Object Storage**.
2. Next to **API Tokens** (in *Account details*), select **Manage** → **Create API Token**.
3. Choose a **permission** level:
   - **Object Read & Write** — typical for an app that reads/writes objects in specific buckets.
   - **Admin Read & Write** — also create/delete buckets (more than an app usually needs).
4. Optionally scope the token to **specific buckets**.
5. Create it and copy the **Access Key ID** and **Secret Access Key** — shown once.

You'll also see the **S3 endpoint**:

```
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

## How the client uses them

The [`AvantiPoint.Aspire.Cloudflare.R2`](../guides/r2.md) client registers an `IAmazonS3` configured for R2 (`ForcePathStyle`, region `auto`, the right checksum settings). It reads the connection string the AppHost injects — which points at MinIO locally and at the real R2 endpoint (with these credentials) in production. **Your application code is identical in both cases:**

```csharp
builder.AddR2Client("uploads");   // inject IAmazonS3 anywhere
```

:::tip
Least privilege: give the app **Object Read & Write** scoped to just the buckets it uses. Reserve bucket creation/deletion for the provisioning API token.
:::

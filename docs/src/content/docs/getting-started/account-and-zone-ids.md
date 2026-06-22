---
title: "Account ID & Zone ID"
---


Cloudflare uses two identifiers you'll provide to the integration.

## Account ID

Your **Account ID** identifies your Cloudflare account. It's required for all provisioning (R2 buckets, Workers/Containers, Pages).

**From the dashboard:**

1. Go to the [Cloudflare dashboard](https://dash.cloudflare.com).
2. On the **Account Home** page, select the menu (•••) next to your account name and choose **Copy account ID**.
   - Alternatively, open **Workers & Pages** — the **Account details** panel on the right shows the **Account ID** with a *Click to copy* link.

It also appears in the dashboard URL: `https://dash.cloudflare.com/<ACCOUNT_ID>`.

The integration reads it from the `CLOUDFLARE_ACCOUNT_ID` environment variable (see [Quickstart](quickstart.md)).

## Zone ID

A **Zone ID** identifies a single domain in your account. You only need it for **custom domains** — attaching a hostname to a Container or Pages app with `WithCustomDomain(zoneId, hostname)`.

**From the dashboard:**

1. Go to the [Cloudflare dashboard](https://dash.cloudflare.com) and select the **domain** you want to use.
2. On the domain's **Overview** tab, find the **API** section (lower-right).
3. Under **Zone ID**, select *Click to copy*.

:::note
The Zone ID is **per-domain**. If you attach custom domains across multiple zones, you'll pass the matching Zone ID for each hostname.
:::


## Where these are used

```csharp
// Account id: from CLOUDFLARE_ACCOUNT_ID (or a parameter you pass to AddCloudflareEnvironment).
builder.AddCloudflareEnvironment();

// Zone id: supply it as a parameter and attach it per custom domain.
var zone = builder.AddDeploymentParameter("zone-id");
api.WithCustomDomain("api.example.com", zone);
```

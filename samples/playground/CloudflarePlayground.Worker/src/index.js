// A hand-authored Cloudflare Worker. Runs locally via `wrangler dev` (Miniflare) during `aspire run`
// and deploys with `wrangler deploy` during `aspire deploy`.
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    return Response.json({
      worker: "cloudflare-playground-worker",
      path: url.pathname,
      message: "Hello from a Cloudflare Worker, orchestrated by .NET Aspire.",
    });
  },
};

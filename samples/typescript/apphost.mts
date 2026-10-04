if (process.env.CLOUDFLARE_INTEROP_TEST === "1") {
    await import("./validate.mjs");
}

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const cloudflare = await builder.addCloudflareEnvironment();
const database = await cloudflare.addD1DatabaseInEnvironment('database').runAsEmulator();
await cloudflare.addKvNamespaceInEnvironment('cache').runAsEmulator();
await cloudflare.addQueueInEnvironment('queue').runAsEmulator();
await cloudflare.addVectorizeIndexInEnvironment('vectors', 384).runAsEmulator();

console.log(`Cloudflare D1 resource: ${await database.getResourceName()}`);
await builder.build().run();

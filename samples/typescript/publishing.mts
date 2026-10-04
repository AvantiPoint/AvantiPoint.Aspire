import assert from 'node:assert/strict';
import { createBuilder } from './.aspire/modules/aspire.mjs';

// The integration fixture owns these randomly named resources and their cleanup.
const name = process.env.CLOUDFLARE_PUBLISH_TEST_NAME;
const directory = process.env.CLOUDFLARE_PUBLISH_TEST_DIRECTORY;
assert.ok(name && /^ap-aspire-it-[a-f0-9]{27}$/.test(name));
assert.ok(directory);

const builder = await createBuilder();
assert.equal(await (await builder.executionContext()).isRunMode(), false, 'This fixture only publishes real Cloudflare services');
const environment = await builder.addCloudflareEnvironment();
await environment.addR2BucketInEnvironment('uploads', { bucketName: name }).runAsEmulator().allowDeletion();
await environment.addCloudflareWorkerInEnvironment(name, directory);
await builder.build().run();

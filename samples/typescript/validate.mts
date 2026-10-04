import assert from 'node:assert/strict';
import { createBuilder, VectorizeMetric } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const token = await builder.addDeploymentParameter('test-token', { secret: true });
const environment = await builder.addCloudflareEnvironment({ name: 'test-cloudflare', apiToken: token });
const database = await environment.addD1DatabaseInEnvironment('database', { databaseName: 'app-data' })
    .runAsEmulator().withLocationHint('weur').withAccessToken(token);
assert.equal(await database.getResourceName(), 'database');
const cache = await builder.addKvNamespace('cache').runAsEmulator().withAccessToken(token);
const queue = await environment.addQueueInEnvironment('queue').runAsEmulator();
const vectors = await environment.addVectorizeIndexInEnvironment('vectors', 384, { metric: VectorizeMetric.Cosine })
    .runAsEmulator();
const bucket = await environment.addR2BucketInEnvironment('bucket').withLocationHint('weur');
assert.equal(await cache.getResourceName(), 'cache');
assert.equal(await queue.getResourceName(), 'queue');
assert.equal(await vectors.getResourceName(), 'vectors');
assert.equal(await bucket.getResourceName(), 'bucket');

let callbackCompleted = false;
const worker = await environment.addCloudflareWorkerInEnvironment('worker', '.', {
    configure: async options => {
        assert.equal(await options.port.get(), 8787);
        await options.port.set(8791);
        assert.equal(await options.port.get(), 8791);
        callbackCompleted = true;
    }
});
assert.ok(callbackCompleted, 'The .NET options callback must complete before AddCloudflareWorker returns');
assert.equal(await worker.getResourceName(), 'worker');
await worker.withReference(database);
const hyperdrive = await database.publishAsHyperdriveWithParameter('hyperdrive', token).withCachingDisabled();
await worker.withHyperdrive(hyperdrive);

const pages = await builder.addViteApp('pages', '.');
await pages.publishAsCloudflarePagesInEnvironment(environment, {
    configure: async options => {
        await options.skipBuild.set(true);
        await options.outputDirectory.set('dist');
        assert.equal(await options.skipBuild.get(), true);
        assert.equal(await options.outputDirectory.get(), 'dist');
    }
});
await pages.withCustomDomain('test.example.com', 'test-zone');
console.log('TypeScript interop passed: resource identity, references, enum, overloads and reentrant options callbacks.');
// This consumer intentionally exercises only model construction. Do not build/run/deploy the model.
process.exit(0);

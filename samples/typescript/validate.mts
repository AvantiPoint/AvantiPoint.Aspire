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
const ai = await builder.addCloudflareAI('ai', {
    configure: async options => {
        await options.chatModel.set('@cf/meta/llama-3.1-8b-instruct');
        assert.equal(await options.chatModel.get(), '@cf/meta/llama-3.1-8b-instruct');
    }
});
assert.equal(await ai.getResourceName(), 'ai');

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
const api = await builder.addProject('api', '../playground/CloudflarePlayground.Api/CloudflarePlayground.Api.csproj');
await api.publishAsCloudflareContainerInEnvironment(environment, {
    configure: async options => {
        await options.port.set(8081);
        await options.maxInstances.set(2);
        assert.equal(await options.port.get(), 8081);
        assert.equal(await options.maxInstances.get(), 2);
    }
});
await api.withCustomDomainParameters(token, token);
await api.withCustomDomainHostnameParameter(token, 'test-zone');
await api.withCustomDomainZoneParameter('test.example.com', token);
console.log('TypeScript interop passed: resource identity, references, enum, overloads and Worker/Pages/AI/Container options callbacks.');
// This consumer intentionally exercises only model construction. Do not build/run/deploy the model.
process.exit(0);

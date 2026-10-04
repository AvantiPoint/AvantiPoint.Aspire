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
        await options.projectName.set('interop-pages');
        await options.branch.set('production');
        await options.buildCommand.set('npm run build');
        assert.equal(await options.skipBuild.get(), true);
        assert.equal(await options.outputDirectory.get(), 'dist');
        assert.equal(await options.projectName.get(), 'interop-pages');
        assert.equal(await options.branch.get(), 'production');
        assert.equal(await options.buildCommand.get(), 'npm run build');
    }
});
await pages.withCustomDomain('test.example.com', 'test-zone');
const api = await builder.addProject('api', '../playground/CloudflarePlayground.Api/CloudflarePlayground.Api.csproj');
await api.withHttpEndpoint({ name: 'http' });
await pages.withEnvironment('VITE_API_URL', api.getEndpoint('http'));
await pages.withEnvironment('PUBLIC_BUILD_CONFIG', '{"mode":"production"}');
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
const postgresHyperdrive = await builder.addPostgres('postgres-source')
    .publishAsHyperdriveWithParameter('postgres-hyperdrive', token);
assert.equal(await postgresHyperdrive.getResourceName(), 'postgres-hyperdrive');
let environmentCallbackCompleted = false;
const configProbe = await builder.addExecutable('config-probe', 'node', '.', ['--version'])
    .withEnvironmentCallback(async context => {
        assert.equal(await context.executionContext().isRunMode(), true);
        await context.environment().set('PUBLIC_BUILD_CONFIG', '{"mode":"production"}');
        environmentCallbackCompleted = true;
    });
await configProbe.createExecutionConfiguration().withEnvironmentVariablesConfig().build(builder.executionContext());
assert.ok(environmentCallbackCompleted, 'Environment callbacks must execute across the TypeScript-to-.NET connection');
console.log('TypeScript interop passed: resource identity, references, enum, overloads, Worker/Pages/AI/Container options and environment callbacks.');
// This consumer intentionally exercises only model construction. Do not build/run/deploy the model.
process.exit(0);

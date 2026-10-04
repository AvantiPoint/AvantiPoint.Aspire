import type { CollectionEntry } from 'astro:content';
import { createHash } from 'node:crypto';

// Bump when the renderer or bundled branding changes. Content edits also change the hash.
export const socialImageVersion = 'v2';
export const socialFormats = [
  { kind: 'og', width: 1200, height: 630 },
  { kind: 'square', width: 1200, height: 1200 },
  { kind: 'twitter', width: 1200, height: 600 },
] as const;

const graphics: Record<string, string> = {
  index: 'cloud', overview: 'network',
  'account-and-zone-ids': 'ids', 'api-tokens': 'key', introduction: 'deployment',
  prerequisites: 'check', quickstart: 'rocket', 'r2-credentials': 'keys', wrangler: 'terminal',
  r2: 'bucket', d1: 'database', ai: 'ai', vectorize: 'vectors', kv: 'kv', queues: 'queue',
  hyperdrive: 'database-network', workers: 'code', containers: 'container', pages: 'browser',
  'custom-domains': 'globe', 'deployment-parameters': 'sliders',
};

// Page content stays authoritative; never turn code examples or credentials into previews.
export function socialDetails(entry: CollectionEntry<'docs'>) {
  const id = !entry.id || entry.id === 'index' ? 'index' : entry.id.replace(/\/$/, '');
  const title = entry.data.title;
  const description = entry.data.description || (id === '404'
    ? 'This documentation page could not be found. Explore the AvantiPoint Aspire integration guides.'
    : `Learn about ${title} with AvantiPoint Aspire hosting and client integrations.`);
  const examples = entry.body?.includes('syncKey="apphost-language"') ?? false;
  const section = id.startsWith('cloudflare/guides/') ? 'Cloudflare services'
    : id.startsWith('cloudflare/getting-started/') ? 'Cloudflare getting started'
    : id.startsWith('extensions/') ? 'Aspire extensions' : 'Cloud integrations';
  const graphic = graphics[id.split('/').at(-1)!] || 'cloud';
  const imageId = id;
  const imageHash = createHash('sha256')
    .update(JSON.stringify({ title, description, section, examples, graphic, socialImageVersion }))
    .digest('hex').slice(0, 12);
  return {
    id, title, description, section, graphic, imageId, imageHash,
    badge: examples ? 'C# + TypeScript AppHosts' : 'Guides + configuration',
    alt: `Blue AvantiPoint logo linked to the ${graphic} symbol for ${title}.`,
    twitterAlt: `AvantiPoint Aspire card titled ${title}, with a topic description and the blue AvantiPoint logo.`,
  };
}

export function socialImages(entry: CollectionEntry<'docs'>) {
  const details = socialDetails(entry);
  return socialFormats.map((format) => ({
    ...format,
    path: `social/${socialImageVersion}/${details.imageId}/${format.kind}-${details.imageHash}.png`,
    alt: format.kind === 'twitter' ? details.twitterAlt : details.alt,
  }));
}

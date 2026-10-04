import type { CollectionEntry } from 'astro:content';

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
  return {
    id, title, description, section,
    badge: examples ? 'C# + TypeScript AppHosts' : 'Guides + configuration',
    imagePath: `social/${id === '404' ? 'index' : id}.png`,
    alt: id === '404' ? 'AvantiPoint Aspire documentation with the blue AvantiPoint logo.'
      : `AvantiPoint Aspire: ${title}. ${section} guide with the blue AvantiPoint logo.`,
  };
}

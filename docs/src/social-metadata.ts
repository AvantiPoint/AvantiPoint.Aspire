import { defineRouteMiddleware } from '@astrojs/starlight/route-data';
import { getEntry } from 'astro:content';
import { socialDetails, socialImages } from './social';

export const onRequest = defineRouteMiddleware(async (context) => {
  const route = context.locals.starlightRoute;
  const social = socialDetails(route.entry);
  const base = import.meta.env.BASE_URL.replace(/\/$/, '');
  const canonical = new URL(context.url.pathname, context.site);
  canonical.search = '';
  canonical.hash = '';
  const imageEntry = social.id === '404' ? (await getEntry('docs', 'index'))! : route.entry;
  const images = socialImages(imageEntry).map((image) => ({
    ...image, url: new URL(`${base}/${image.path}`, context.site).href,
  }));
  const twitter = images.find(({ kind }) => kind === 'twitter')!;
  const title = social.id === 'index' ? social.title : `${social.title} | AvantiPoint Aspire`;
  const properties: Record<string, string> = {
    'og:title': social.title,
    'og:type': social.id === 'index' ? 'website' : 'article',
    'og:url': canonical.href,
    'og:description': social.description,
    'og:locale': 'en_US',
  };
  const names: Record<string, string> = {
    description: social.description,
    'twitter:card': 'summary_large_image',
    'twitter:title': social.title,
    'twitter:description': social.description,
    'twitter:image': twitter.url,
    'twitter:image:alt': twitter.alt,
  };
  // Replace defaults in Starlight's resolved head rather than rendering a second set.
  route.head = route.head.filter(({ tag, attrs }) =>
    tag !== 'title' && !(tag === 'link' && attrs?.rel === 'apple-touch-icon') && !(tag === 'meta' &&
      (String(attrs?.property).startsWith('og:image') ||
       String(attrs?.property) in properties || String(attrs?.name) in names)));
  route.head.push({ tag: 'title', content: title });
  for (const [property, content] of Object.entries(properties)) {
    route.head.push({ tag: 'meta', attrs: { property, content } });
  }
  // OG arrays are ordered preferences, not responsive srcset or a platform-routing mechanism.
  // Each root image must be immediately followed by its own structured properties.
  for (const image of images.filter(({ kind }) => kind !== 'twitter')) {
    for (const [property, content] of Object.entries({
      'og:image': image.url, 'og:image:secure_url': image.url, 'og:image:type': 'image/png',
      'og:image:width': String(image.width), 'og:image:height': String(image.height),
      'og:image:alt': image.alt,
    })) route.head.push({ tag: 'meta', attrs: { property, content } });
  }
  route.head.push({ tag: 'link', attrs: {
    rel: 'apple-touch-icon', sizes: '1493x1493',
    href: new URL(`${base}/favicon.png`, context.site).href,
  } });
  for (const [name, content] of Object.entries(names)) {
    route.head.push({ tag: 'meta', attrs: { name, content } });
  }
});

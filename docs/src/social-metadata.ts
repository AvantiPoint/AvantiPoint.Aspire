import { defineRouteMiddleware } from '@astrojs/starlight/route-data';
import { socialDetails } from './social';

export const onRequest = defineRouteMiddleware((context) => {
  const route = context.locals.starlightRoute;
  const social = socialDetails(route.entry);
  const base = import.meta.env.BASE_URL.replace(/\/$/, '');
  const canonical = new URL(context.url.pathname, context.site);
  canonical.search = '';
  canonical.hash = '';
  const image = new URL(`${base}/${social.imagePath}`, context.site).href;
  const title = social.id === 'index' ? social.title : `${social.title} | AvantiPoint Aspire`;
  const properties: Record<string, string> = {
    'og:title': title,
    'og:type': social.id === 'index' ? 'website' : 'article',
    'og:url': canonical.href,
    'og:description': social.description,
    'og:image': image,
    'og:image:type': 'image/png',
    'og:image:width': '1200',
    'og:image:height': '630',
    'og:image:alt': social.alt,
  };
  const names: Record<string, string> = {
    description: social.description,
    'twitter:card': 'summary_large_image',
    'twitter:title': title,
    'twitter:description': social.description,
    'twitter:image': image,
    'twitter:image:alt': social.alt,
  };
  // Replace defaults in Starlight's resolved head rather than rendering a second set.
  route.head = route.head.filter(({ tag, attrs }) =>
    tag !== 'title' && !(tag === 'meta' &&
      (String(attrs?.property).startsWith('og:image') ||
       String(attrs?.property) in properties || String(attrs?.name) in names)));
  route.head.push({ tag: 'title', content: title });
  for (const [property, content] of Object.entries(properties)) {
    route.head.push({ tag: 'meta', attrs: { property, content } });
  }
  for (const [name, content] of Object.entries(names)) {
    route.head.push({ tag: 'meta', attrs: { name, content } });
  }
});

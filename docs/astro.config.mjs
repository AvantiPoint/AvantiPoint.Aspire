// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  site: 'https://avantipoint.github.io',
  base: '/AvantiPoint.Aspire',
  integrations: [
    starlight({
      title: 'AvantiPoint Aspire',
      description:
        'AvantiPoint hosting and client integrations for modeling, provisioning and deploying Cloudflare resources with Aspire.',
      logo: { src: './src/assets/logo.png', alt: 'AvantiPoint' },
      favicon: '/favicon.png',
      customCss: ['./src/styles/brand.css'],
      routeMiddleware: './src/social-metadata.ts',
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/AvantiPoint/AvantiPoint.Aspire',
        },
      ],
      sidebar: [
        'overview',
        {
          label: 'Cloudflare',
          items: [
            {
              label: 'Getting Started',
              items: [
                'cloudflare/getting-started/introduction',
                'cloudflare/getting-started/prerequisites',
                'cloudflare/getting-started/account-and-zone-ids',
                'cloudflare/getting-started/api-tokens',
                'cloudflare/getting-started/r2-credentials',
                'cloudflare/getting-started/wrangler',
                'cloudflare/getting-started/quickstart',
              ],
            },
            {
              label: 'Services',
              items: [
                'cloudflare/guides/r2',
                'cloudflare/guides/d1',
                'cloudflare/guides/ai',
                'cloudflare/guides/vectorize',
                'cloudflare/guides/kv',
                'cloudflare/guides/queues',
                'cloudflare/guides/hyperdrive',
                'cloudflare/guides/workers',
                'cloudflare/guides/containers',
                'cloudflare/guides/pages',
                'cloudflare/guides/custom-domains',
              ],
            },
          ],
        },
        {
          label: 'Extensions',
          items: ['extensions/deployment-parameters'],
        },
      ],
    }),
  ],
});

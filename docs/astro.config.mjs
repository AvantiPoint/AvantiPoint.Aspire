// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  site: 'https://avantipoint.github.io',
  base: '/AvantiPoint.Aspire',
  integrations: [
    starlight({
      title: 'Aspire for Cloudflare',
      description:
        'Provision and deploy your .NET Aspire applications to Cloudflare — R2, Workers, Containers and Pages.',
      logo: { src: './src/assets/logo.png', alt: 'AvantiPoint' },
      favicon: '/favicon.png',
      customCss: ['./src/styles/brand.css'],
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/AvantiPoint/AvantiPoint.Aspire',
        },
      ],
      editLink: {
        baseUrl: 'https://github.com/AvantiPoint/AvantiPoint.Aspire/edit/master/docs/',
      },
      sidebar: [
        {
          label: 'Getting Started',
          items: [
            'getting-started/introduction',
            'getting-started/prerequisites',
            'getting-started/account-and-zone-ids',
            'getting-started/api-tokens',
            'getting-started/r2-credentials',
            'getting-started/wrangler',
            'getting-started/quickstart',
          ],
        },
        {
          label: 'Guides',
          items: [
            'guides/r2',
            'guides/d1',
            'guides/ai',
            'guides/vectorize',
            'guides/kv',
            'guides/queues',
            'guides/workers',
            'guides/containers',
            'guides/pages',
            'guides/custom-domains',
          ],
        },
      ],
    }),
  ],
});

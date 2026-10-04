import { getCollection, type CollectionEntry } from 'astro:content';
import type { APIRoute, GetStaticPaths } from 'astro';
import sharp from 'sharp';
import { resolve } from 'node:path';
import { socialDetails } from '../../social';

export const prerender = true;
export const getStaticPaths = (async () => {
  const entries = await getCollection('docs', ({ data }) => !data.draft);
  return entries.map((entry) => ({ params: { slug: socialDetails(entry).id }, props: { entry } }));
}) satisfies GetStaticPaths;

const escape = (value: string) => value.replace(/[&<>"']/g, (char) =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' })[char]!);

export const GET: APIRoute = async ({ props }) => {
  const social = socialDetails(props.entry as CollectionEntry<'docs'>);
  const fontfile = resolve('src/assets/fonts/Inter.ttf');
  async function text(value: string, size: number, width: number, color: string, weight = 'normal') {
    return sharp({ text: {
      text: `<span foreground="${color}" weight="${weight}">${escape(value)}</span>`,
      font: `Inter ${size}`, fontfile, width, dpi: 72, rgba: true,
    } }).png().toBuffer();
  }
  const title = await text(social.title, 64, 780, '#ffffff', 'bold');
  const titleHeight = (await sharp(title).metadata()).height!;
  const description = await text(social.description, 28, 930, '#c8cce6');
  const descriptionHeight = (await sharp(description).metadata()).height!;
  if (titleHeight > 160 || descriptionHeight > 120) {
    throw new Error(`Social card text exceeds safe area: ${social.id}`);
  }
  const background = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630">
    <defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#101020"/><stop offset="1" stop-color="#1e1e56"/></linearGradient></defs>
    <rect width="1200" height="630" fill="url(#bg)"/>
    <path d="M860 0L1200 340M980 0L1200 220M740 0L1200 460" stroke="#4040f7" stroke-opacity=".22" stroke-width="2"/>
    <rect x="72" y="171" width="58" height="6" rx="3" fill="#7272ff"/>
    <path d="M72 516H1128" stroke="#64648d" stroke-opacity=".5"/>
    <rect x="72" y="550" width="8" height="24" rx="4" fill="#7272ff"/>
  </svg>`);
  const logo = await sharp(resolve('src/assets/logo.png')).resize(150, 150).png().toBuffer();
  const image = await sharp(background).composite([
    { input: await text('AvantiPoint Aspire', 30, 700, '#ffffff', 'bold'), left: 72, top: 63 },
    { input: logo, left: 978, top: 38 },
    { input: await text(social.section.toUpperCase(), 19, 880, '#a9a9ff', 'bold'), left: 72, top: 130 },
    { input: title, left: 72, top: 207 },
    { input: description, left: 72, top: 225 + titleHeight },
    { input: await text(social.badge, 23, 700, '#e3e3ff'), left: 96, top: 548 },
    { input: await text('DOCUMENTATION', 17, 220, '#a9a9c5', 'bold'), left: 936, top: 552 },
  ]).png().toBuffer();
  return new Response(new Uint8Array(image), { headers: { 'Content-Type': 'image/png' } });
};

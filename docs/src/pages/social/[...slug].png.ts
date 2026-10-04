import { getCollection, type CollectionEntry } from 'astro:content';
import type { APIRoute, GetStaticPaths } from 'astro';
import sharp from 'sharp';
import { resolve } from 'node:path';
import { socialDetails, socialImages } from '../../social';

export const prerender = true;
export const getStaticPaths = (async () => {
  const entries = await getCollection('docs', ({ data }) => !data.draft);
  return entries.flatMap((entry) => socialImages(entry).map((format) => ({
    params: { slug: format.path.slice('social/'.length).replace(/\.png$/, '') },
    props: { entry, format },
  })));
}) satisfies GetStaticPaths;

const escape = (value: string) => value.replace(/[&<>"']/g, (char) =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' })[char]!);

export const GET: APIRoute = async ({ props }) => {
  const social = socialDetails(props.entry as CollectionEntry<'docs'>);
  const format = props.format as ReturnType<typeof socialImages>[number];
  const illustrations: Record<string, string> = {
    cloud: '<path d="M42 128H151a30 30 0 0 0 0-60 48 48 0 0 0-91-14 38 38 0 0 0-18 74Z"/>',
    deployment: '<path d="M44 101h103a25 25 0 0 0 0-50 40 40 0 0 0-75-14 32 32 0 0 0-28 64ZM100 177v-53M80 145l20-21 20 21"/>',
    'database-network': '<ellipse cx="135" cy="40" rx="40" ry="15"/><path d="M95 40v63c0 20 80 20 80 0V40M95 74c0 20 80 20 80 0M95 85H46v52M95 103l-20 54"/><circle cx="46" cy="157" r="20"/><circle cx="75" cy="173" r="14"/>',
    network: '<path d="M48 48L150 50 100 150Z"/><circle cx="48" cy="48" r="19"/><circle cx="150" cy="50" r="19"/><circle cx="100" cy="150" r="19"/>',
    ids: '<rect x="20" y="28" width="160" height="135" rx="16"/><circle cx="61" cy="77" r="18"/><path d="M38 123q23-38 46 0M108 69h46M108 95h33M108 122h46"/>',
    key: '<circle cx="61" cy="74" r="37"/><path d="M88 100l73 73 20-20-17-17-15 15-15-15 15-15-24-24"/>',
    keys: '<circle cx="62" cy="54" r="26"/><path d="M62 80v75h25v-18H62M104 101l63 63 18-18-15-15-13 13"/><circle cx="99" cy="76" r="26"/>',
    check: '<rect x="30" y="22" width="140" height="155" rx="16"/><path d="M61 78l24 24 53-55M61 140h76"/>',
    rocket: '<path d="M67 124C63 67 109 28 166 27c-1 58-38 104-95 107ZM76 75l-34 9-17 43 41-4M119 129l-1 41-43 6 9-35M52 149l-22 22"/><circle cx="129" cy="65" r="14"/>',
    terminal: '<rect x="18" y="30" width="164" height="138" rx="16"/><path d="M46 72l31 28-31 28M96 130h48"/>',
    bucket: '<ellipse cx="100" cy="49" rx="65" ry="22"/><path d="M35 49l16 103q49 30 98 0l16-103M61 98q39 24 78 0"/>',
    database: '<ellipse cx="100" cy="42" rx="65" ry="22"/><path d="M35 42v111c0 30 130 30 130 0V42M35 80c0 30 130 30 130 0M35 117c0 30 130 30 130 0"/>',
    ai: '<rect x="52" y="52" width="96" height="96" rx="21"/><path d="M77 52V27M123 52V27M77 148v25M123 148v25M52 77H27M52 123H27M148 77h25M148 123h25"/><circle cx="81" cy="93" r="6"/><circle cx="119" cy="93" r="6"/><path d="M81 117h38"/>',
    vectors: '<path d="M29 171V30M29 171h142M29 171L162 39"/><circle cx="65" cy="123" r="8"/><circle cx="85" cy="72" r="8"/><circle cx="110" cy="132" r="8"/><circle cx="145" cy="83" r="8"/>',
    kv: '<rect x="25" y="34" width="150" height="36" rx="9"/><rect x="25" y="83" width="150" height="36" rx="9"/><rect x="25" y="132" width="150" height="36" rx="9"/><path d="M73 34v36M73 83v36M73 132v36"/>',
    queue: '<rect x="18" y="64" width="43" height="72" rx="9"/><rect x="78" y="64" width="43" height="72" rx="9"/><rect x="138" y="64" width="43" height="72" rx="9"/><path d="M61 100h17M121 100h17M33 32h130l-17-16M163 32l-17 16"/>',
    code: '<path d="M61 53L20 100l41 47M139 53l41 47-41 47M119 34L81 166"/>',
    container: '<path d="M26 58L100 20l74 38v86l-74 37-74-37ZM26 58l74 38 74-38M100 96v85M64 39l74 38"/>',
    browser: '<rect x="20" y="26" width="160" height="148" rx="15"/><path d="M20 63h160M49 44h1M70 44h1M91 44h1M43 92h69v53H43M132 95h26M132 120h26M132 145h26"/>',
    globe: '<circle cx="100" cy="100" r="77"/><ellipse cx="100" cy="100" rx="33" ry="77"/><path d="M23 100h154M34 60h132M34 140h132"/>',
    sliders: '<path d="M37 28v144M100 28v144M163 28v144"/><rect x="22" y="55" width="30" height="31" rx="8"/><rect x="85" y="118" width="30" height="31" rx="8"/><rect x="148" y="74" width="30" height="31" rx="8"/>',
  };
  const graphic = illustrations[social.graphic] || illustrations.cloud;
  if (format.kind !== 'twitter') {
    // A wordless illustration remains recognizable in compact and square crops.
    // Core artwork fits within the central 630x630 region of the wide image.
    const scale = format.kind === 'square' ? 1.8 : 1;
    const xOffset = (1200 - 1200 * scale) / 2;
    const yOffset = format.height / 2 - 315 * scale;
    const background = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="${format.height}">
      <defs><radialGradient id="bg"><stop stop-color="#292971"/><stop offset="1" stop-color="#101020"/></radialGradient></defs>
      <rect width="1200" height="${format.height}" fill="url(#bg)"/>
      <g transform="translate(${xOffset} ${yOffset}) scale(${scale})">
        <circle cx="600" cy="315" r="265" fill="none" stroke="#4040f7" stroke-opacity=".25" stroke-width="2"/>
        <rect x="315" y="110" width="570" height="410" rx="58" fill="#17172f" stroke="#4c4c94" stroke-width="2"/>
        <path d="M565 310H646" stroke="#8585ff" stroke-width="6"/>
        <circle cx="605" cy="310" r="10" fill="#8585ff"/>
        <rect x="636" y="217" width="200" height="200" rx="35" fill="#252559"/>
        <g transform="translate(657 238) scale(.79)" stroke="#c9c9ff" stroke-width="8" fill="none" stroke-linejoin="round" stroke-linecap="round">${graphic}</g>
      </g>
    </svg>`);
    const logo = await sharp(resolve('src/assets/logo.png')).resize(250 * scale, 250 * scale).png().toBuffer();
    const image = await sharp(background).composite([{
      input: logo, left: Math.round(xOffset + 340 * scale), top: Math.round(yOffset + 185 * scale),
    }]).png().toBuffer();
    return new Response(new Uint8Array(image), { headers: { 'Content-Type': 'image/png' } });
  }
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
    <rect x="96" y="171" width="58" height="6" rx="3" fill="#7272ff"/>
    <path d="M72 516H1128" stroke="#64648d" stroke-opacity=".5"/>
    <rect x="96" y="550" width="8" height="24" rx="4" fill="#7272ff"/>
  </svg>`);
  const logo = await sharp(resolve('src/assets/logo.png')).resize(150, 150).png().toBuffer();
  const wide = await sharp(background).composite([
    { input: await text('AvantiPoint Aspire', 30, 700, '#ffffff', 'bold'), left: 96, top: 63 },
    { input: logo, left: 954, top: 38 },
    { input: await text(social.section.toUpperCase(), 19, 880, '#a9a9ff', 'bold'), left: 96, top: 130 },
    { input: title, left: 96, top: 207 },
    { input: description, left: 96, top: 225 + titleHeight },
    { input: await text(social.badge, 23, 700, '#e3e3ff'), left: 120, top: 548 },
    { input: await text('DOCUMENTATION', 17, 220, '#a9a9c5', 'bold'), left: 936, top: 552 },
  ]).png().toBuffer();
  // X gets its own 2:1 asset through twitter:image, independently of OG selection.
  const image = await sharp(wide).extract({ left: 0, top: 15, width: 1200, height: 600 }).png().toBuffer();
  return new Response(new Uint8Array(image), { headers: { 'Content-Type': 'image/png' } });
};

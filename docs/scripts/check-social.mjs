import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import { join } from 'node:path';
import sharp from 'sharp';

// Inspect the actual static output, including Starlight defaults and the generated 404.
async function* htmlFiles(directory) {
  for (const item of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, item.name);
    if (item.isDirectory()) yield* htmlFiles(path);
    else if (item.name.endsWith('.html')) yield path;
  }
}
const titles = new Set();
const descriptions = new Set();
const images = new Set();
let pages = 0;
for await (const path of htmlFiles('dist')) {
  const html = await readFile(path, 'utf8');
  const head = html.slice(html.indexOf('<head>'), html.indexOf('</head>'));
  const metadata = new Map();
  for (const tag of head.matchAll(/<meta\b[^>]*>/g)) {
    const attrs = Object.fromEntries([...tag[0].matchAll(/([\w:-]+)="([^"]*)"/g)].map((m) => [m[1], m[2]]));
    const key = attrs.property || attrs.name;
    if (!key?.startsWith('og:') && !key?.startsWith('twitter:') && key !== 'description') continue;
    assert(!metadata.has(key), `${path}: duplicate ${key}`);
    assert(attrs.content, `${path}: empty ${key}`);
    metadata.set(key, attrs.content);
  }
  for (const key of ['og:title', 'og:description', 'og:url', 'og:type', 'og:site_name', 'og:image',
    'og:image:type', 'og:image:width', 'og:image:height', 'og:image:alt', 'twitter:card',
    'twitter:title', 'twitter:description', 'twitter:image', 'twitter:image:alt', 'description']) {
    assert(metadata.has(key), `${path}: missing ${key}`);
  }
  assert.equal(metadata.get('twitter:card'), 'summary_large_image');
  assert.equal(metadata.get('twitter:title'), metadata.get('og:title'));
  assert.equal(metadata.get('twitter:description'), metadata.get('og:description'));
  assert.equal(metadata.get('description'), metadata.get('og:description'));
  assert.equal(metadata.get('twitter:image'), metadata.get('og:image'));
  assert.equal(metadata.get('twitter:image:alt'), metadata.get('og:image:alt'));
  assert.equal(metadata.get('og:image:type'), 'image/png');
  assert.equal(metadata.get('og:image:width'), '1200');
  assert.equal(metadata.get('og:image:height'), '630');
  const canonicals = [...head.matchAll(/<link\b[^>]*rel="canonical"[^>]*>/g)];
  assert.equal(canonicals.length, 1, `${path}: canonical count`);
  const canonical = canonicals[0][0].match(/href="([^"]+)"/)[1];
  assert.equal(canonical, metadata.get('og:url'));
  assert.equal(new URL(canonical).origin, 'https://avantipoint.github.io');
  assert(new URL(canonical).pathname.startsWith('/AvantiPoint.Aspire/'));
  assert(!new URL(canonical).search && !new URL(canonical).hash);
  assert.equal([...head.matchAll(/<title>/g)].length, 1);
  const image = new URL(metadata.get('og:image'));
  assert.equal(image.origin, 'https://avantipoint.github.io');
  assert(image.pathname.startsWith('/AvantiPoint.Aspire/social/'));
  const bytes = await readFile(join('dist', image.pathname.slice('/AvantiPoint.Aspire/'.length)));
  assert(bytes.length < 5_000_000, `${path}: image too large`);
  const info = await sharp(bytes).metadata();
  assert.equal(info.format, 'png');
  assert.equal(info.width, 1200);
  assert.equal(info.height, 630);
  if (!path.endsWith('404.html')) {
    assert(!titles.has(metadata.get('og:title')), `${path}: reused page title`);
    assert(!descriptions.has(metadata.get('og:description')), `${path}: reused description`);
    assert(!images.has(image.href), `${path}: reused topic image`);
    titles.add(metadata.get('og:title'));
    descriptions.add(metadata.get('og:description'));
    images.add(image.href);
    pages++;
  }
}
assert(pages > 0, 'No static docs pages checked');
console.log(`Verified ${pages} unique page previews and the 404 fallback: metadata, canonical URLs, PNG dimensions and file sizes.`);

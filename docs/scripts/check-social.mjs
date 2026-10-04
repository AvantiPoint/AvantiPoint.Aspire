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
  assert(Buffer.byteLength(html) < 1_000_000, `${path}: exceeds Messages main-resource guidance`);
  const head = html.slice(html.indexOf('<head>'), html.indexOf('</head>'));
  const metadata = new Map();
  const imageGroups = [];
  let currentImage;
  for (const tag of head.matchAll(/<meta\b[^>]*>/g)) {
    const attrs = Object.fromEntries([...tag[0].matchAll(/([\w:-]+)="([^"]*)"/g)].map((m) => [m[1], m[2]]));
    const key = attrs.property || attrs.name;
    if (!key?.startsWith('og:') && !key?.startsWith('twitter:') && key !== 'description') continue;
    assert(attrs.content, `${path}: empty ${key}`);
    if (key === 'og:image') {
      currentImage = { url: attrs.content, properties: [] };
      imageGroups.push(currentImage);
    } else if (key.startsWith('og:image:')) {
      assert(currentImage, `${path}: orphaned ${key}`);
      assert(!(key in currentImage), `${path}: repeated image property ${key}`);
      currentImage[key] = attrs.content;
      currentImage.properties.push(key);
    } else {
      currentImage = undefined;
      assert(!metadata.has(key), `${path}: duplicate ${key}`);
    }
    if (!metadata.has(key)) metadata.set(key, attrs.content);
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
  assert.notEqual(metadata.get('twitter:image'), metadata.get('og:image'));
  assert.equal(metadata.get('og:locale'), 'en_US');
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
  assert.equal(imageGroups.length, 2, `${path}: wide-first OG image array with square alternative`);
  const checkImage = async (url, width, height, kind) => {
    const image = new URL(url);
    assert.equal(image.protocol, 'https:');
    assert.equal(image.origin, 'https://avantipoint.github.io');
    assert(image.pathname.startsWith('/AvantiPoint.Aspire/social/v2/'));
    assert(new RegExp(`/${kind}-[a-f0-9]{12}\\.png$`).test(image.pathname), `${path}: unversioned image`);
    const bytes = await readFile(join('dist', image.pathname.slice('/AvantiPoint.Aspire/'.length)));
    // A conservative project budget, not a claimed WhatsApp/Discord limit.
    assert(bytes.length < 250_000, `${path}: social image exceeds 250 kB budget`);
    const info = await sharp(bytes).metadata();
    assert.equal(info.format, 'png');
    assert.equal(info.width, width);
    assert.equal(info.height, height);
    return bytes.length;
  };
  let imageBytes = 0;
  for (const [index, group] of imageGroups.entries()) {
    assert.deepEqual(group.properties, ['og:image:secure_url', 'og:image:type',
      'og:image:width', 'og:image:height', 'og:image:alt'], `${path}: image association/order`);
    assert.equal(group['og:image:secure_url'], group.url);
    assert.equal(group['og:image:type'], 'image/png');
    assert.equal(group['og:image:width'], '1200');
    assert.equal(group['og:image:height'], index === 0 ? '630' : '1200');
    assert(group['og:image:alt']);
    imageBytes += await checkImage(group.url, 1200, index === 0 ? 630 : 1200, index === 0 ? 'og' : 'square');
  }
  imageBytes += await checkImage(metadata.get('twitter:image'), 1200, 600, 'twitter');
  const appleIcons = [...head.matchAll(/<link\b[^>]*rel="apple-touch-icon"[^>]*>/g)];
  assert.equal(appleIcons.length, 1);
  const icon = new URL(appleIcons[0][0].match(/href="([^"]+)"/)[1]);
  assert.equal(icon.href, 'https://avantipoint.github.io/AvantiPoint.Aspire/favicon.png');
  const iconBytes = await readFile('dist/favicon.png');
  const iconInfo = await sharp(iconBytes).metadata();
  assert.equal(iconInfo.width, iconInfo.height);
  assert(iconInfo.width >= 108);
  assert(imageBytes + iconBytes.length < 10_000_000, `${path}: preview resource budget`);
  const image = new URL(metadata.get('og:image'));
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
console.log(`Verified ${pages} page previews and the 404 fallback: ordered OG image groups (1200x630 + 1200x1200), separate 1200x600 Twitter cards, icon, versioned HTTPS URLs and byte budgets.`);

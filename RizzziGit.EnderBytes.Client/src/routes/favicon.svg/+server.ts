import FS from 'fs';
import Path from 'path';

export const prerender = false;

export const _sizes: number[] = [16, 32, 64, 72, 96, 128, 144];

export function _getUrl(size: number) {
  return `/favicon.svg?size=${size}`;
}

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);

  let svg = FS.readFileSync(Path.join(process.cwd(), 'static/favicon-original.svg')).toString('utf-8');
  const size = Number.parseInt(searchParams.get('size') ?? '0') || _sizes.at(-1);

  svg = svg
    .replaceAll('width="16px"', `width="${size}px"`)
    .replaceAll('height="16px"', `height="${size}px"`);

  return new Response(svg, {
    headers: {
      'Content-Type': 'image/svg+xml',
      'Content-Length': `${svg.length}`
    }
  });
}

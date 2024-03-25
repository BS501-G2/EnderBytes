import FS from 'fs'

export const prerender = true

export async function GET(request: Request) {
  const query = new URL(request.url).searchParams

  let svg = FS.readFileSync('./static/icon.svg').toString('utf-8')

  const size = query.has('size') ? Number.parseInt(query.get('size')!) : undefined

  if (size) {
    svg = svg.replaceAll('width="16px"', `width="${size}px"`).replaceAll('height="16px"', `height="${size}px"`)
    // svg = svg.replaceAll('viewbox="0 0 16 16"', `viewbox="0 0 ${size} ${}"`)
  }

  return new Response(svg, {
    headers: {
      'Content-Type': 'image/svg+xml',
      'Content-Length': `${svg.length}`
    }
  })
}

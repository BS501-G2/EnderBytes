import FS from 'fs'
import Path from 'path'

export const prerender = true

export const _sizes: number[] = [16, 32, 64, 72, 96, 128, 144]

export function _getUrl(size: number) {
	return `/dynamic-icons/${size}x${size}/favicon.svg`
}

export function entries() {
	return _sizes.map((size) => ({
		size: `${size}x${size}`
	}))
}

export async function GET(request: Request) {
	const { pathname } = new URL(request.url)

	let svg = FS.readFileSync(Path.join(process.cwd(), 'static/favicon.svg')).toString('utf-8')

	let width: number
	let height: number

	{
		const rawSize = pathname.split('/')[2].split('x', 2)

		width = Number.parseInt(rawSize[0]) || 0
		height = Number.parseInt(rawSize[1]) || width
	}

	svg = svg.replaceAll('width="16px"', `width="${width || 16}px"`).replaceAll('height="16px"', `height="${height || 16}px"`)

	return new Response(svg, {
		headers: {
			'Content-Type': 'image/svg+xml',
			'Content-Length': `${svg.length}`
		}
	})
}

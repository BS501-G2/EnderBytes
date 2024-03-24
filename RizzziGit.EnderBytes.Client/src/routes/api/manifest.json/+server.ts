import { json } from '@sveltejs/kit';
import * as Manifest from '$lib/values'

export const prerender = true

export function GET() {
  return json({
    lang: 'en',
    dir: 'ltr',
    id: 'enderdrive',
    name: Manifest.APP_NAME,
    short_name: Manifest.APP_NAME,
    description: Manifest.APP_TAGLINE,
    icons: [
      { src: '/favicon.svg', 'sizes': '16x16', 'type': 'image/png' },
      { src: '/favicon.svg', 'sizes': '64x64', 'type': 'image/png' },
      { src: '/favicon.svg', 'sizes': '128x128', 'type': 'image/png' }
    ],
    categories: ['education', 'utilities'],
    display_override: ['fullscreen', 'minimal-ui'],
    display: 'standalone',
    launch_handler: {
      client_mode: 'focus-existing'
    },
    orientation: 'any',
    shortcuts: [
      { name: 'Upload', 'url': '/upload' },
      { name: 'Starred', 'url': '/starred' },
      { name: 'Feed', 'url': '/Feed' }
    ],
    start_url: '/app',
    theme_color: '#0000ff',
    'background-color': '#0000ff'
  }, {
    headers: {
      'content-type': 'application/manifest+json'
    }
  })
}

import { json } from '@sveltejs/kit';
import * as Manifest from '$lib/manifest'

export function GET() {
  return json({
    'lang': 'en',
    'dir': 'ltr',
    'id': 'enderbytes',
    'name': Manifest.APP_NAME,
    'short_name': Manifest.APP_NAME,
    'description': Manifest.APP_TAGLINE,
    'icons': [
      { 'src': '/favicon.png', 'sizes': '16x16', 'type': 'image/png' },
      { 'src': '/favicon.png', 'sizes': '64x64', 'type': 'image/png' },
      { 'src': '/favicon.png', 'sizes': '128x128', 'type': 'image/png' }
    ],
    'categories': ['education', 'utilities'],
    'display_override': ['fullscreen', 'minimal-ui'],
    'display': 'standalone',
    'launch_handler': {
      'client_mode': 'focus-existing'
    },
    'orientation': 'any',
    'shortcuts': [
      { 'name': 'Upload', 'url': '/upload' },
      { 'name': 'Starred', 'url': '/starred' },
      { 'name': 'Feed', 'url': '/Feed' }
    ],
    'scope': 'http://localhost:8081/app',
    'start_url': '/',
    'theme_color': '#0000ff',
    'background-color': '#0000ff'
  }, {
    headers: {
      'content-type': 'application/manifest+json'
    }
  })
}

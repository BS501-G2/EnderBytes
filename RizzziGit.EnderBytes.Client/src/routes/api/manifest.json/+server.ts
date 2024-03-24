import { json } from '@sveltejs/kit';
import { colors } from '$lib/color-schemes';
import { Locale, strings } from '$lib/locale';

export const prerender = true

const icon = (size: number) => ({ src: `/favicon.svg?size=${size}`, 'sizes': `${size}x${size}`, 'type': 'image/svg+xml' })
const icons = () => [icon(16), icon(32), icon(64), icon(72), icon(96), icon(128), icon(144)]

const shortcut = (name: string, url: string) => ({ name, url, icons: icons() })

export function GET(request: Request) {
  const { searchParams } = new URL(request.url)

  const locale = (<Locale>searchParams.get('locale')) ?? Locale.en_US
  const localeStrings = strings[locale]

  return json({
    lang: locale,
    dir: 'ltr',
    name: localeStrings.AppName,
    short_name: localeStrings.AppName,
    description: localeStrings.AppTagline,
    icons: icons(),
    categories: ['education', 'utilities'],
    display_override: ['window-controls-overlay', 'fullscreen', 'minimal-ui'],
    display: 'standalone',
    launch_handler: {
      client_mode: 'focus-existing'
    },
    orientation: 'any',
    shortcuts: [
      shortcut('Upload', '/upload'),
      shortcut('Starred', '/starred'),
      shortcut('Feed', '/feed')
    ],
    start_url: '/app',
    theme_color: colors.green.primaryContainer,
    background_color: colors.green.background
  }, {
    headers: {
      'content-type': 'application/manifest+json'
    }
  })
}

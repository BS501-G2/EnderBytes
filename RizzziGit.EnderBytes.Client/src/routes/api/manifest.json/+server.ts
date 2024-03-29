import { json } from '@sveltejs/kit';
import { ColorScheme, colors } from '$lib/color-schemes';
import { LocaleKey, Locale, bindLocalizedString } from '$lib/locale';
import { _sizes } from '../../dynamic-icons/[size]/favicon.svg/+server';

export const prerender = true

const icon = (size: number) => ({ src: `/dynamic-icons/${size}x${size}/favicon.svg`, 'sizes': `${size}x${size}`, 'type': 'image/svg+xml' })
const icons = () => _sizes.map((size) => icon(size))

const shortcut = (name: string, url: string) => ({ name, url, icons: icons() })

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url)

  const locale = (<Locale | null>searchParams.get('locale')) ?? Locale.en_US
  const color = (<ColorScheme | null>searchParams.get('theme')) ?? ColorScheme.Ender

  const getString = bindLocalizedString(() => locale)

  return json({
    lang: locale,
    dir: 'ltr',
    name: getString(LocaleKey.AppName),
    short_name: getString(LocaleKey.AppName),
    description: getString(LocaleKey.AppTagline),
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
    theme_color: `#${colors[color].primaryContainer.toString(16)}`,
    background_color: `#${colors[color].background.toString(16)}`
  }, {
    headers: {
      'content-type': 'application/manifest+json'
    }
  })
}

export enum Theme {
  Default = 'default',
  Blue = 'blue'
}

export type ThemeColorValues = Record<ThemeColorKey, string>
export type ThemeColorKey = (
  (typeof THEME_COLOR_PRIMARY) |
  (typeof THEME_COLOR_PRIMARY_CONTAINER) |
  (typeof THEME_COLOR_ON_PRIMARY) |
  (typeof THEME_COLOR_ON_PRIMARY_CONTAINER)
)

export const THEME_COLOR_PRIMARY = 'primary'
export const THEME_COLOR_PRIMARY_CONTAINER = 'primaryContainer'
export const THEME_COLOR_ON_PRIMARY = 'onPrimary'
export const THEME_COLOR_ON_PRIMARY_CONTAINER = 'onPrimaryContainer'

export const colors: Record<Theme, ThemeColorValues> = {
  [Theme.Default]: {
    [THEME_COLOR_PRIMARY]: '#000000',
    [THEME_COLOR_PRIMARY_CONTAINER]: '#000000',
    [THEME_COLOR_ON_PRIMARY]: '#000000',
    [THEME_COLOR_ON_PRIMARY_CONTAINER]: '#000000'
  },

  [Theme.Blue]: {
    [THEME_COLOR_PRIMARY]: '#000001',
    [THEME_COLOR_PRIMARY_CONTAINER]: '#000001',
    [THEME_COLOR_ON_PRIMARY]: '#000001',
    [THEME_COLOR_ON_PRIMARY_CONTAINER]: '#000001'
  }
}

export function serializeThemeColorsIntoInlineStyle(theme: Theme) {
  const color = colors[theme]

  let style = ""

  for (const key in color) {
    if (style.length !== 0) {
      style += '; '
    }

    style += `--${key}: ${color[<ThemeColorKey> key]}`
  }

  return style
}

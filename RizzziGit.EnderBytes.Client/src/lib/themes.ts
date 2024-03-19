export enum Theme {
  Ender = 'green'
}

export type ThemeColorValues = Record<ThemeColorKey, string>
export type ThemeColorKey = (
  (typeof THEME_COLOR_PRIMARY) |
  (typeof THEME_COLOR_PRIMARY_CONTAINER) |
  (typeof THEME_COLOR_ON_PRIMARY) |
  (typeof THEME_COLOR_ON_PRIMARY_CONTAINER) |
  (typeof THEME_COLOR_BACKGROUND) |
  (typeof THEME_COLOR_ON_BACKGROUND)
)

export const THEME_COLOR_PRIMARY = 'primary'
export const THEME_COLOR_PRIMARY_CONTAINER = 'primaryContainer'
export const THEME_COLOR_ON_PRIMARY = 'onPrimary'
export const THEME_COLOR_ON_PRIMARY_CONTAINER = 'onPrimaryContainer'
export const THEME_COLOR_BACKGROUND = 'background'
export const THEME_COLOR_ON_BACKGROUND = 'onBackground'

export const intColorToHex = (color: number): string => `#${color.toString(16)}`

// const a = /^#([0-9a-fA-F]{2}|[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/

export const colors: Record<Theme, ThemeColorValues> = {
  [Theme.Ender]: {
    [THEME_COLOR_PRIMARY]: '#37812e',
    [THEME_COLOR_PRIMARY_CONTAINER]: '#86c058',
    [THEME_COLOR_ON_PRIMARY]: '#ffffffff',
    [THEME_COLOR_ON_PRIMARY_CONTAINER]: '#ffffffff',
    [THEME_COLOR_BACKGROUND]: '#86c05852',
    [THEME_COLOR_ON_BACKGROUND]: '#000000ff'
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

import { colors as enderColorScheme } from "./color-scheme/ender"

export enum ColorScheme {
  Ender = 'green'
}

export type ColorValues = Record<ColorKey, string>
export type ColorKey = (
  (typeof THEME_COLOR_PRIMARY) |
  (typeof THEME_COLOR_ON_PRIMARY) |
  (typeof THEME_COLOR_PRIMARY_CONTAINER) |
  (typeof THEME_COLOR_ON_PRIMARY_CONTAINER) |

  (typeof THEME_COLOR_PRIMARY_VARIANT) |
  (typeof THEME_COLOR_ON_PRIMARY_VARIANT) |
  (typeof THEME_COLOR_PRIMARY_VARIANT_CONTAINER) |
  (typeof THEME_COLOR_ON_PRIMARY_VARIANT_CONTAINER) |

  (typeof THEME_COLOR_BACKGROUND) |
  (typeof THEME_COLOR_ON_BACKGROUND)
)

export const THEME_COLOR_PRIMARY = 'primary'
export const THEME_COLOR_ON_PRIMARY = 'onPrimary'
export const THEME_COLOR_PRIMARY_CONTAINER = 'primaryContainer'
export const THEME_COLOR_ON_PRIMARY_CONTAINER = 'onPrimaryContainer'

export const THEME_COLOR_PRIMARY_VARIANT = 'primaryVariant'
export const THEME_COLOR_ON_PRIMARY_VARIANT = 'onPrimaryVariant'
export const THEME_COLOR_PRIMARY_VARIANT_CONTAINER = 'primaryVariantContainer'
export const THEME_COLOR_ON_PRIMARY_VARIANT_CONTAINER = 'onPrimaryVariantContainer'

export const THEME_COLOR_BACKGROUND = 'background'
export const THEME_COLOR_ON_BACKGROUND = 'onBackground'

export const intColorToHex = (color: number): string => `#${color.toString(16)}`

// const a = /^#([0-9a-fA-F]{2}|[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/

export const colors: Record<ColorScheme, ColorValues> = {
  [ColorScheme.Ender]: enderColorScheme()
}

export function serializeThemeColorsIntoInlineStyle(theme: ColorScheme) {
  const color = colors[theme]

  let style = ""

  for (const key in color) {
    if (style.length !== 0) {
      style += '; '
    }

    style += `--${key}: ${color[<ColorKey> key]}`
  }

  return style
}

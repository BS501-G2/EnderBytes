import { colors as enderColorScheme } from "./color-scheme/ender"

export enum ColorScheme {
  Ender = 'green'
}

export type ColorValues = Record<ColorKey, number>
export enum ColorKey {
  Primary = 'primary',
  OnPrimary = 'onPrimary',
  PrimaryContainer = 'primaryContainer',
  OnPrimaryContainer = 'onPrimaryContainer',

  PrimaryVariant = 'primaryVariant',
  OnPrimaryVariant = 'onPrimaryVariant',
  PrimaryVariantContainer = 'primaryVariantContainer',
  OnPrimaryVariantContainer = 'onPrimaryVariantContainer',

  Background = 'background',
  OnBackground = 'onBackground',
}
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

    style += `--${key}: #${color[<ColorKey>key].toString(16).padStart(8, '0')}`
  }

  return style
}

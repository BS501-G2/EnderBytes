import { colors as enderColorScheme } from "./color-scheme/ender"
import { colors as enderDarkColorScheme } from "./color-scheme/ender-dark"

export enum ColorScheme {
  Ender = 'green',
  EnderDark = 'green-dark'
}

export type ColorValues = Record<ColorKey, number>
export enum ColorKey {
  Primary = 'primary',
  OnPrimary = 'onPrimary',
  PrimaryContainer = 'primaryContainer',
  OnPrimaryContainer = 'onPrimaryContainer',

  PrimaryVariant = 'primaryVariant',
  OnPrimaryVariant = 'onPrimaryVariant',
  PrimaryContainerVariant = 'primaryContainerVariant',
  OnPrimaryContainierVariant = 'onPrimaryContainerVariant',

  Background = 'background',
  BackgroundVariant = 'backgroundVariant',
  OnBackground = 'onBackground',
  OnBackgroundVariant = 'onBackgroundVariant',

  Error = 'error',
  ErrorBackground = 'errorBackground',
  OnError = 'onError',

  Warning = 'warning',
  WarningBackground = 'warningBackground',
  OnWarning = 'onWarning',

  Info = 'info',
  InfoBackground = 'infoBackground',
  OnInfo = 'onInfo',

  Shadow = 'shadow'
}
export const intColorToHex = (color: number): string => `#${color.toString(16)}`

// const a = /^#([0-9a-fA-F]{2}|[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/

export const colors: Record<ColorScheme, ColorValues> = {
  [ColorScheme.Ender]: enderColorScheme(),
  [ColorScheme.EnderDark]: enderDarkColorScheme()
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

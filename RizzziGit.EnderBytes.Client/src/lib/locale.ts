import { locale as en_US } from './locale/en-us'

export enum Locale {
  en_US = 'en_US'
}

export type LocaleValues = Record<LocaleKey, string>
export type LocaleKey = (
  (typeof LOCALE_APP_NAME) |
  (typeof LOCALE_APP_TAGLINE)
)

export const LOCALE_APP_NAME = 'AppName'
export const LOCALE_APP_TAGLINE = 'AppTagline'

export const strings: Record<Locale, LocaleValues> = {
  [Locale.en_US]: en_US()
}

export function localizedString(locale: Locale, key: LocaleKey, params?: Record<string, string>) {
  let string: string = strings[locale][key]

  if (params != null) {
    for (const paramKey in params) {
      string = string.replaceAll(`\${${paramKey}}`, params[paramKey])
    }
  }

  return string
}

export function bindLocalizedString(locale: () => Locale): (key: LocaleKey, params?: Record<string, string>) => string {
  return (key, params) => localizedString(locale(), key, params)
}

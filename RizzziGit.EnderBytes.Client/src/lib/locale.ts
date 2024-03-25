import { locale as en_US } from './locale/en-us'

export enum Locale {
  en_US = 'en_US'
}

export type LocaleValues = Record<LocaleKey, string>
export enum LocaleKey {
  AppName,
  AppTagline,

  AltIconSite,
  AltIconSearch,

  SearchBarPlaceholder,
  SearchBannerPlaceholderText
}

export const strings: Record<Locale, LocaleValues> = {
  [Locale.en_US]: en_US()
}

export function getString<L extends Locale, K extends LocaleKey>(locale: L, key: K, params?: Record<string, string>): (typeof strings)[L][K] {
  let string: string = strings[locale][key]

  if (params != null) {
    for (const paramKey in params) {
      string = string.replaceAll(`\${${paramKey}}`, params[paramKey])
    }
  }

  return string
}

export function bindLocalizedString(locale: () => Locale): (key: LocaleKey, params?: Record<string, string>) => string {
  return (key, params) => getString(locale(), key, params)
}

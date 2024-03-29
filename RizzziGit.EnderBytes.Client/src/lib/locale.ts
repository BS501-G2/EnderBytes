import { locale as en_US } from './locale/en-us'
import { locale as tl_PH } from './locale/tl-ph'

export enum Locale {
  en_US = 'en_US',
  tl_PH = 'tl_PH'
}

export type LocaleValues = Record<LocaleKey, string>
export enum LocaleKey {
  AppName,
  AppTagline,

  AltIconSite,
  AltIconSearch,

  SearchBarPlaceholder,
  SearchBannerPlaceholderText,
  AuthLoginPageUsernamePlaceholder,
  AuthLoginPagePasswordPlaceholder,
  AuthLoginPageSubmit,
}

export const strings: Record<Locale, LocaleValues> = {
  [Locale.en_US]: en_US(),
  [Locale.tl_PH]: tl_PH()
}

export function getString<L extends Locale, K extends LocaleKey>(locale: L, key: K, params?: Record<string, string>): (typeof strings)[L][K] {
  let string: string = strings?.[locale]?.[key] ?? `\${${key}}`

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

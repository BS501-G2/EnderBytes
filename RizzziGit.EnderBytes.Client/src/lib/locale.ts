import { getLocale as en_US } from './locale/en-us'

export enum Locale {
  en_US = 'en_US'
}

export type LocaleValues = Record<LocaleKey, string>
export type LocaleKey = (
  (typeof LOCALE_APP_NAME)
)

export const LOCALE_APP_NAME = 'AppName'

export const strings:  Record<Locale, LocaleValues> = {
  [Locale.en_US]: en_US()
}

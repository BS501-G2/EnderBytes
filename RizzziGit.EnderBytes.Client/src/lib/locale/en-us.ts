import {
  LOCALE_APP_NAME,
  LOCALE_APP_TAGLINE,

  type LocaleValues
} from "$lib/locale";

export const locale: () => LocaleValues = () => ({
  [LOCALE_APP_NAME]: "EnderDrive",
  [LOCALE_APP_TAGLINE]: 'Secure and Private File Storage and Sharing Website for Melchora Aquino Elementary School.'
})

import { LOCALE_APP_NAME, type LocaleValues } from "$lib/locale";

export const getLocale: () => LocaleValues = () => ({
  [LOCALE_APP_NAME]: "EnderDrive"
})

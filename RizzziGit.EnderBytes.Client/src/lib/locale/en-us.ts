import {
  LocaleKey,

  type LocaleValues
} from "$lib/locale";

export const locale: () => LocaleValues = () => ({
  [LocaleKey.AppName]: "EnderDrive",
  [LocaleKey.AppTagline]: 'Secure and Private File Storage and Sharing Website for Melchora Aquino Elementary School.',

  [LocaleKey.AltIconSite]: 'Webite Icon',
  [LocaleKey.AltIconSearch]: 'Search Icon',

  [LocaleKey.SearchBarPlaceholder]: 'Search...',
  [LocaleKey.SearchBannerPlaceholderText]: 'Begin typing search keywords.'
})

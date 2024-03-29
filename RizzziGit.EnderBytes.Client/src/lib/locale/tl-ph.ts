import {
  LocaleKey,

  type LocaleValues
} from "$lib/locale";

import { locale as en_US } from './en-us'

export const locale: () => LocaleValues = () => (Object.assign(en_US(), {
}))

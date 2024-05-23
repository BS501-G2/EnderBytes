import { type LocaleValues } from '$lib/locale.svelte';

import { locale as en_US } from './en-us';

export const locale: () => LocaleValues = () => ({
  ...en_US()
});

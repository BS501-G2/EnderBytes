<script lang="ts" context="module">
	import { derived, get, writable, type Writable } from 'svelte/store';
	import { locale as en_US } from './locale/en-us';
	import { locale as tl_PH } from './locale/tl-ph';
	import type { Snippet } from 'svelte';

	export enum Locale {
		en_US = 'en_US',
		tl_PH = 'tl_PH'
	}

	export type LocaleValues = Record<LocaleKey, (...args: string[]) => string>;
	export enum LocaleKey {
		AppName,
		AppTagline,

		AltIconSite,
		AltIconSearch,

		SearchBarPlaceholder,
		SearchBannerPlaceholderText,
		AuthLoginPageUsernamePlaceholder,
		AuthLoginPagePasswordPlaceholder,
		AuthLoginPageSubmit
	}

	export const strings: Record<Locale, LocaleValues> = {
		[Locale.en_US]: en_US(),
		[Locale.tl_PH]: tl_PH()
	};

	export function getLocale() {
		let locale = (localStorage.getItem('locale') as Locale | null) ?? Locale.en_US;

		if (!(locale in Locale)) {
			locale = Locale.en_US;
		}

		return locale;
	}

	export function setLocale(locale: Locale) {
		localStorage.setItem('locale', locale);
	}

	export function getString(key: LocaleKey, locale?: Locale | null, ...args: string[]): string {
		if (locale == null) {
			locale = getLocale();
		}

		return strings?.[locale]?.[key]?.(...args) ?? `\${${locale},${key}}`;
	}
</script>

<script lang="ts">
	let {
		locale = null,
		string,
		children
	}: { locale?: Locale | null; string: [LocaleKey, ...string[]][]; children?: Snippet<[string[]]> } = $props();
</script>

{#if children}
	{@render children(string.map(([key, ...args]) => getString(key, locale, ...args)))}
{/if}

<script lang="ts">
	import '@fortawesome/fontawesome-free/css/all.min.css';

	import Locale, { LocaleKey } from '$lib/locale.svelte';
	import { ColorKey, ResetCSS, Title, titleString } from '@rizzzi/svelte-commons';
	import ColorScheme, { getColorHex } from '../../../../svelte-commons/dist/color-scheme.svelte';
</script>

<svelte:head>
	<meta name="theme-color" content={getColorHex(ColorKey.PrimaryContainer)} />
	<title>{$titleString}</title>
</svelte:head>

<Locale string={[[LocaleKey.AppName]]}>
	{#snippet children([appName])}
		<Title title={appName} />
	{/snippet}
</Locale>
<ResetCSS />
<ColorScheme />

<slot />

<style lang="scss">
	:root {
		font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;

		background-color: var(--background);

		color: var(--onBackground);

		min-width: 320px;
	}

	:global(body) {
		-webkit-app-region: drag;

		margin: unset;
		min-height: 100vh;
		background-color: var(--background);
	}
</style>

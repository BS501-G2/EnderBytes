<script lang="ts">
	import '@fortawesome/fontawesome-free/css/all.min.css';
	import './reset.scss';

	import { onMount } from 'svelte';

	import { RootState } from '$lib/states/root-state';
	import { ColorScheme, ColorKey, serializeThemeColorsIntoInlineStyle } from '$lib/color-schemes';
	import Locale, { LocaleKey } from '$lib/locale.svelte';
	import Title, { titleString } from '$lib/widgets/title.svelte';

	const rootState = RootState.state;

	function loadSettings() {
		$rootState.theme = <ColorScheme | null>localStorage.getItem('theme') ?? ColorScheme.Ender;
	}

	function saveSettings() {
		localStorage.setItem('theme', $rootState.theme);
	}

	onMount(() => {
		loadSettings();

		rootState.subscribe((value) => {
			saveSettings();
		});
	});
</script>

<svelte:head>
	<meta name="theme-color" content={$rootState.getColorHex(ColorKey.PrimaryContainer)} />
	<title>{$titleString}</title>

	{@html `<style>
	:root {
		${serializeThemeColorsIntoInlineStyle($rootState.theme)}
	}

	body {
		margin: unset;
		min-height: 100vh;
		background-color: var(--background);
	}
	</style>`}
</svelte:head>

<Locale string={[[LocaleKey.AppName]]}>
	{#snippet children([appName])}
		<Title title={appName} />
	{/snippet}
</Locale>
<slot />

<style lang="scss">
	:root {
		font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;

		background-color: var(--background);

		color: var(--onBackground);

		min-width: 320px;
	}
</style>

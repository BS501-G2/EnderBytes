<script lang="ts" context="module">
	import { get, writable, type Writable } from 'svelte/store';

	export enum ViewMode {
		Unset = 0,

		Mobile = 0b0000001,
		Desktop = 0b0000010,
		Browser = 0b0000100,
		Standalone = 0b0001000,
		Fullscreen = 0b0010000,
		MinimalUI = 0b0100000,
		OverlayControls = 0b1000000
	}

	export const viewMode: Writable<ViewMode> = writable(ViewMode.Unset);

	function onResize() {
		let newViewMode: ViewMode = ViewMode.Unset;

		newViewMode |= window.matchMedia('(max-width: 768px)').matches
			? ViewMode.Mobile
			: ViewMode.Desktop;

		newViewMode |= window.matchMedia('(display-mode: minimal-ui)').matches
			? ViewMode.MinimalUI
			: window.matchMedia('(display-mode: window-controls-overlay)').matches
				? ViewMode.OverlayControls
				: window.matchMedia('(display-mode: standalone)').matches
					? ViewMode.Standalone
					: ViewMode.Browser;

		if (newViewMode != get(viewMode)) {
			viewMode.set(newViewMode);
		}
	}

	interface CheckerEntry {
		checker: () => void;
	}

	const boundFunction: Writable<Array<CheckerEntry>> = writable([]);
</script>

<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { Snippet } from 'svelte';

	const { desktop, mobile }: { desktop?: Snippet; mobile?: Snippet } = $props();

	const entry: CheckerEntry = { checker: onResize };

	function check() {
		const index = $boundFunction.indexOf(entry);

		if (index != 0) {
			return;
		}

		$boundFunction[index].checker();
	}

	onMount(() => {
		$boundFunction.push(entry);
		$boundFunction = $boundFunction;

		check();
	});

	onDestroy(() => {
		$boundFunction.splice($boundFunction.indexOf(entry), 1);
		$boundFunction = $boundFunction;
	});
</script>

<svelte:window onresize={onResize} />

{#if $viewMode & ViewMode.Desktop}
	{#if desktop}
		{@render desktop()}
	{/if}
{:else if $viewMode & ViewMode.Mobile}
	{#if mobile}
		{@render mobile()}
	{/if}
{/if}

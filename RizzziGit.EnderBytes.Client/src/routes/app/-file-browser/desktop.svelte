<script lang="ts">
	import type { FileBrowserState, FileResource } from '../file-browser.svelte';
	import MainPanel from './desktop/main-panel.svelte';
	import SidePanel from './desktop/side-panel.svelte';

	let { fileBrowserState = $bindable(), selection = $bindable() }: { fileBrowserState: FileBrowserState, selection: FileResource[] } = $props();

</script>

<div>
	<MainPanel {fileBrowserState} bind:selection />

	{#if !fileBrowserState.hideSidePanel}
		<SidePanel {fileBrowserState} />
	{/if}
</div>

<style lang="scss">
	div {
		display: flex;
		flex-direction: row;

		flex-grow: 1;
		min-height: 100%;
		box-sizing: border-box;

		padding: 16px;
		gap: 16px;
	}
</style>

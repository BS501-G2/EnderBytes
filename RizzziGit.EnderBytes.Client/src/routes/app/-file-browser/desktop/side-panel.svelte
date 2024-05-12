<script lang="ts">
	import type { FileBrowserState } from '../../file-browser.svelte';

	let { fileBrowserState = $bindable() }: { fileBrowserState: FileBrowserState } = $props();
</script>

<div class="side-panel">
	{#if !fileBrowserState.isLoading}
		<h3>{fileBrowserState.title ?? fileBrowserState.file?.name ?? 'Root'}</h3>
	{/if}
</div>

<style lang="scss">
	div.side-panel {
		border-radius: 8px;

		display: flex;
		flex-direction: column;

		min-width: 320px;
		max-width: 320px;

		padding: 16px;
		box-sizing: border-box;

		background-color: var(--backgroundVariant);
	}
</style>

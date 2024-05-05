<script lang="ts">
	import Loading, { type Size } from './LoadingSpinner.svelte';

	export let loadingSize: Size | null = null;

	interface $$Slots {
		default: {};
		'without-spinner': {};
	}
</script>

<div class="loading-page">
	{#if $$slots['without-spinner']}
		<slot name="without-spinner" />
	{:else}
		<div
			class="loading-icon {loadingSize == null ? 'nosize' : ''}"
			style={loadingSize == null ? '' : `width: ${loadingSize}px; height: ${loadingSize}px;`}
		>
			<Loading size={loadingSize} />
		</div>
		{#if $$slots.default}
			<slot />
		{/if}
	{/if}
</div>

<style lang="scss">
	div.loading-page {
		width: 100%;
		height: 100%;

		display: flex;
		flex-direction: column;
		justify-content: center;
		align-items: center;

		> div.loading-icon.nosize {
			max-width: 128px;
			max-height: 128px;
		}
	}
</style>

<script lang="ts">
	import Loading, { type Size } from './loading-spinner.svelte';

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
		<Loading size={loadingSize ?? '128px'} />
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
	}
</style>

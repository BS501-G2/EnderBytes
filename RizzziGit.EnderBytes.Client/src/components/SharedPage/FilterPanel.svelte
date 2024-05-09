<script lang="ts">
	import type { SharedFileListFilter } from '../SharedPage.svelte';

	export let filter: SharedFileListFilter;
</script>

<div class="filter-panel">
	<h2>Filter</h2>
</div>

<style lang="scss">
	div.filter-panel {
		min-width: 320px;
		max-width: 320px;

		border-radius: 8px;
		padding: 16px;
		background-color: var(--backgroundVariant);
	}
</style>

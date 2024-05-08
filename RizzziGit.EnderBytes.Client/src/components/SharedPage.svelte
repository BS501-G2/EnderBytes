<script lang="ts" context="module">
	export interface SharedFileListGroup {
		fileAccesses: any[];
	}

	export interface SharedFileListFilter {
		authorUserId: number | null;
		domainUserId: number | null;
		targetFileId: number | null;
		isFolder: boolean | null;
		createdAfter: number | null;
		createdBefore: number | null;
	}
</script>

<script lang="ts">
	import FilterPanel from './SharedPage/FilterPanel.svelte';
	import SharedFilesList from './SharedPage/SharedFilesList.svelte';

	let sharedFiles: SharedFileListGroup[] = $state([]);
	let filter: SharedFileListFilter = $state({
		authorUserId: null,
		domainUserId: null,
		targetFileId: null,
		isFolder: null,
		createdAfter: null,
		createdBefore: null
	});
</script>

<div class="page">
	<div class="main-panel">
		<SharedFilesList bind:filter bind:sharedFiles />
	</div>

	<div class="filter-panel">
		<FilterPanel bind:filter />
	</div>
</div>

<style lang="scss">
	div.page {
		display: flex;
		flex-direction: row;
		flex-grow: 1;
		gap: 16px;
		padding: 16px;

		min-height: 100%;
		max-height: 100%;
		max-width: 100%;

		> div.main-panel {
			display: flex;
			flex-direction: column;
			flex-grow: 1;
			align-items: center;
		}

		> div.filter-panel {
			min-width: 320px;
			max-width: 320px;

			border-radius: 8px;
			padding: 16px;
			background-color: var(--backgroundVariant);
		}
	}
</style>

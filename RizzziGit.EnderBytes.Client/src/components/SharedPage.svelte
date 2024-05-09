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
	<SharedFilesList bind:filter bind:sharedFiles />
	<FilterPanel bind:filter />
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
		box-sizing: border-box;

		max-width: 100%;
	}
</style>

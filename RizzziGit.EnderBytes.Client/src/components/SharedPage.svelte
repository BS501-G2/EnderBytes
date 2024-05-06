<script lang="ts" context="module">
	export interface SharedFileGroup {
		fileAccesses: any[];
	}
</script>

<script lang="ts">
	import Awaiter from './Bindings/Awaiter.svelte';
	import { apiFetch } from './Bindings/Client.svelte';
	import SharedFilesGroup from './SharedPage/SharedFilesGroup.svelte';

	async function loading() {}

	let sharedFiles: SharedFileGroup[] = $state([]);
	let nextPage: boolean = true;

	let sharedFilesCount = $derived(
		sharedFiles.map((file) => file.fileAccesses.length).reduce((a, b) => a + b, 0)
	);
</script>

<div class="page">
	<div class="main-panel">
		{#each sharedFiles as { fileAccesses }}
			<SharedFilesGroup {fileAccesses} />
		{/each}
		<Awaiter
			callback={async () => {
				if (!nextPage) return

				const accesses = await apiFetch({ path: '/shares' })

				if (accesses.length < 1) {
					nextPage = false
					return
				}

				let access: any | null
				while ((access = accesses.shift()) != null) {
					if (sharedFiles.length < 1 || sharedFiles[0].fileAccesses[0]?.authorUserId != access.authorUserId) {
						sharedFiles.unshift({
							fileAccesses: [access],
						})
					} else {
						sharedFiles[0].fileAccesses.unshift(access)
					}
				}

				console.log(sharedFiles)
			}}
		/>
	</div>

	<div class="filter-panel">
		<h2>Filter</h2>
	</div>
</div>

<style lang="scss">
	div.page {
		display: flex;
		flex-direction: row;
		flex-grow: 1;
		gap: 16px;
		padding: 16px;

		max-height: 100%;
		max-width: 100%;

		> div.main-panel {
			display: flex;
			flex-direction: column;
			flex-grow: 1;
			// align-items: flex-start;
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

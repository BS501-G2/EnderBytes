<script lang="ts">
	import Awaiter from '../Bindings/Awaiter.svelte';
	import { apiFetch } from '../Bindings/Client.svelte';
	import type { SharedFileListFilter, SharedFileListGroup } from '../SharedPage.svelte';
	import SharedFilesGroup from './SharedFilesGroup.svelte';

	export let filter: SharedFileListFilter;
	export let sharedFiles: SharedFileListGroup[] = [];

	let nextPage: boolean = true;
	let asd: number = 0;
</script>

<div class="main-panel">
	<div class="list-container">
		{#each sharedFiles as { fileAccesses }}
			<SharedFilesGroup {fileAccesses} />
		{/each}
		{#if nextPage}
			<div>
				<Awaiter
					callback={async () => {
						if (!nextPage) return;

						const accesses = await apiFetch({ path: '/shares' });

						if (accesses.length < 1) {
							nextPage = false;
							return;
						}

						for (const accesss of accesses) {
							const group = sharedFiles.findLast(
								(group) => group.fileAccesses[0]?.authorUserId == accesss.authorUserId
							);

							if (!group) {
								sharedFiles.push({
									fileAccesses: [accesss]
								});
							} else {
								group.fileAccesses.push(accesss);
							}
						}
					}}
				/>
			</div>
		{/if}
	</div>
</div>

<style lang="scss">
	div.main-panel {
		display: flex;
		flex-direction: column;
		flex-grow: 1;
		align-items: center;

		overflow-y: auto;

		> div.list-container {
			display: flex;
			flex-direction: column;
			flex-grow: 1;

			max-width: min(768px, 100%);
			min-width: min(768px, 100%);
		}
	}
</style>

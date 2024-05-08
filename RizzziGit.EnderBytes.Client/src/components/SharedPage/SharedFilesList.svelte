<script lang="ts">
	import Awaiter from '../Bindings/Awaiter.svelte';
	import { apiFetch } from '../Bindings/Client.svelte';
	import type { SharedFileListFilter, SharedFileListGroup } from '../SharedPage.svelte';
	import SharedFilesGroup from './SharedFilesGroup.svelte';

	export let filter: SharedFileListFilter;
	export let sharedFiles: SharedFileListGroup[] = [];

	let nextPage: boolean = true;
</script>

<div class="list-container">
	{#each sharedFiles as { fileAccesses }}
		<SharedFilesGroup {fileAccesses} />
	{/each}
	<Awaiter
		callback={async () => {
			if (!nextPage) return;

			const accesses = await apiFetch({ path: '/shares' });

			if (accesses.length < 1) {
				nextPage = false;
				return;
			}

			for (const accesss of accesses) {
        const group = sharedFiles.findLast((group) => group.fileAccesses[0]?.authorUserId == accesss.authorUserId);

        if (!group) {
          sharedFiles.push({
            fileAccesses: [accesss],
          });
        } else {
          group.fileAccesses.push(accesss);
        }
			}

			// let access: any | null
			// while ((access = accesses.shift()) != null) {
			// 	if (sharedFiles.length < 1 || sharedFiles[0].fileAccesses[0]?.authorUserId != access.authorUserId) {
			// 		sharedFiles.unshift({
			// 			fileAccesses: [access],
			// 		})
			// 	} else {
			// 		sharedFiles[0].fileAccesses.unshift(access)
			// 	}
			// }

			// console.log(sharedFiles)
		}}
	/>
</div>

<style lang="scss">
	div.list-container {
		display: flex;
		flex-direction: column;
		flex-grow: 1;
		gap: 16px;

		max-width: min(768px, 100%);
		min-width: min(768px, 100%);
	}
</style>

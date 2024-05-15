<script lang="ts">
	import FileBrowser, {
		getFile,
		getFileAccessList,
		getFilePathChain,
		scanFolder,
		type FileBrowserState,
		type FileResource
	} from '../file-browser.svelte';

	import { page } from '$app/stores';
	import { Title, Awaiter } from "@rizzzi/svelte-commons";

	function parseId(id: string | null) {
		if (id == null) {
			return undefined;
		} else {
			return Number.parseInt(id) || undefined;
		}
	}

	const id = $derived(parseId($page.url.searchParams.get('id')));
</script>

{#key id}
	<Awaiter
		callback={async (): Promise<FileBrowserState & { isLoading: false }> => {
		const file = await getFile(id);

		let fileBrowserState: FileBrowserState & { isLoading: false } = $state({
			isLoading: false,
			files: file.isFolder ? await scanFolder(file) : [],
			pathChain: await getFilePathChain(file),
			access:  await getFileAccessList(file),
			hideControlBar: !file.isFolder,
			file,
			title: id != null ? file.name : 'Home',

			allowCreate: true
		});

		return fileBrowserState
	}}
	>
		<svelte:fragment slot="loading">
			<Title title="My Files" />
			<FileBrowser fileBrowserState={{ isLoading: true }} />
		</svelte:fragment>
		<svelte:fragment slot="success" let:result={fileBrowserState}>
			{#if fileBrowserState.file?.parentId != null}
				<Title title={fileBrowserState.file.name} />
			{:else}
				<Title title="My Files" />
			{/if}
			<FileBrowser {fileBrowserState} />
		</svelte:fragment>
	</Awaiter>
{/key}

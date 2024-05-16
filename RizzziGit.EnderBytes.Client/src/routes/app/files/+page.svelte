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
	import { Title, Awaiter, Dialog } from '@rizzzi/svelte-commons';

	interface FolderListFilter {
		sort?: 'name' | 'ctime' | 'utime';
		desc?: boolean;
		offset?: number;
	}

	function parseId(id: string | null) {
		if (id == null) {
			return undefined;
		} else {
			return Number.parseInt(id) || undefined;
		}
	}

	const id = $derived(parseId($page.url.searchParams.get('id')));

	let filterOpen: boolean = $state(false);
	let filter: FolderListFilter = {};
</script>

{#if filterOpen}
	<Dialog
		onDismiss={() => {
			filterOpen = false;
		}}
	>
		{#snippet head()}
			<h2>Filter</h2>
		{/snippet}
		{#snippet body()}
			<p>Select any metric to filter files.</p>
		{/snippet}
	</Dialog>
{/if}

{#key id}
	<Awaiter
		callback={async (): Promise<FileBrowserState & { isLoading: false }> => {
		const file = await getFile(id);

		let fileBrowserState: FileBrowserState & { isLoading: false } = $state({
			onFilter: async ()  => {
				console.log('opening filter dialog')
				await new Promise<void>((resolve) => setTimeout(resolve, 1000))
				filterOpen = true;
			},

			isLoading: false,
			files: file.isFolder ? await scanFolder(file) : [],
			pathChain: await getFilePathChain(file),
			access:	await getFileAccessList(file),
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

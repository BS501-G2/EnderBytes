<script lang="ts" context="module">
	const filterOpen: Writable<boolean> = writable(false);
</script>

<script lang="ts">
	import FileBrowser, {
		getFile,
		getFileAccessList,
		getFilePathChain,
		scanFolder,
		type FileBrowserState
	} from '../file-browser.svelte';
	import { type ControlBarItem } from '../-file-browser/desktop/main-panel/control-bar.svelte';
	import FilterDialog, { type FolderListFilter } from './filter-dialog.svelte';

	import { page } from '$app/stores';
	import { Title, Awaiter, type AwaiterResetFunction } from '@rizzzi/svelte-commons';
	import { writable, type Writable } from 'svelte/store';

	function parseId(id: string | null) {
		if (id == null) {
			return undefined;
		} else {
			return Number.parseInt(id) || undefined;
		}
	}

	const id = $derived(parseId($page.url.searchParams.get('id')));

	let refresh: Writable<AwaiterResetFunction<null>> = writable();
	let filter: FolderListFilter = $state({});

  refresh.subscribe(console.log)

	const actions: (ControlBarItem & { isLoading: boolean })[] = [
		{
			label: 'Refresh',
			icon: 'fa-solid fa-sync',
			action: async () => {
				console.log('asds');
				await $refresh(true, null);
			},
			group: 'arrangement',
			isLoading: true
		},
		{
			label: 'Filter',
			icon: 'fa-solid fa-filter',
			action: async () => {
				$filterOpen = true;
			},
			group: 'arrangement',
			isLoading: true
		},
		{
			label: 'New Folder',
			icon: 'fa-solid fa-folder',
			action: async () => {},
			isLoading: false,
			group: 'new'
		},
		{
			label: 'Upload',
			icon: 'fa-solid fa-upload',
			action: async () => {},
			isLoading: false,
			group: 'new'
		}
	];
</script>

{#if $filterOpen}
	<FilterDialog
		bind:filter
		onFilterApply={$refresh}
		onDismiss={() => {
			$filterOpen = false;
		}}
	/>
{/if}

{#key id}
	<Awaiter
		bind:reset={$refresh}
		callback={async (): Promise<FileBrowserState & { isLoading: false }> => {
    const file = await getFile(id);

    const [files, pathChain, access] = await Promise.all([
      file.isFolder ? scanFolder(file) : [],
      getFilePathChain(file),
      getFileAccessList(file),
    ])

    let fileBrowserState: FileBrowserState & { isLoading: false } = $state({
      isLoading: false,
      files,
      pathChain,
      access,
      hideControlBar: !file.isFolder,
      file,

      allowCreate: true,
      controlBarActions: actions
    });

    return fileBrowserState
  }}
	>
		{#snippet loading()}
			<Title title="My Files" />
			<FileBrowser
				fileBrowserState={{
					isLoading: true,
					controlBarActions: actions.filter((action) => action.isLoading)
				}}
			/>
		{/snippet}
		{#snippet success({ result: fileBrowserState })}
			{#if fileBrowserState.file?.parentId != null}
				<Title title={fileBrowserState.file.name} />
			{:else}
				<Title title="My Files" />
			{/if}
			<FileBrowser {fileBrowserState} />
		{/snippet}
	</Awaiter>
{/key}

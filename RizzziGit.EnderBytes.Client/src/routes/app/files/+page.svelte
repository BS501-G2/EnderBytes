<script lang="ts" context="module">
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
  import FilterOverlay, {
    type FolderListFilter,
    filterOverlayState
  } from './filter-overlay.svelte';

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
  let title: string | null = $state(null);

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
      action: async ({ currentTarget }) => {
        const bounds = (currentTarget as HTMLElement).getBoundingClientRect();

        $filterOverlayState.enabled = [window.innerWidth - bounds.right, bounds.bottom];
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

  $effect(() => console.log(JSON.stringify($filterOverlayState)));
</script>

{#key title}
  <Title title={title ?? 'My Files'} />
{/key}
<FilterOverlay
  onFilterApply={() => $refresh(true, null)}
  onDismiss={() => {
    $filterOverlayState.enabled = null;
    $filterOverlayState = $filterOverlayState;
  }}
/>

{#key id}
  <Awaiter
    bind:reset={$refresh}
    callback={async (): Promise<FileBrowserState & { isLoading: false }> => {
    const file = await getFile(id);

    const [files, pathChain, access] = await Promise.all([
      file.isFolder ? scanFolder(file, $filterOverlayState.state) : [],
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

  title = id != null ? fileBrowserState.file?.name ?? null : null

    return fileBrowserState
  }}
  >
    {#snippet loading()}
      <FileBrowser
        fileBrowserState={{
          isLoading: true,
          controlBarActions: actions.filter((action) => action.isLoading)
        }}
      />
    {/snippet}
    {#snippet success({ result: fileBrowserState })}
      <FileBrowser {fileBrowserState} />
    {/snippet}
  </Awaiter>
{/key}

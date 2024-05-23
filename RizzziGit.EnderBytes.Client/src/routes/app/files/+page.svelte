<script lang="ts">
  import FileBrowser, {
    getFile,
    getFileAccessList,
    getFilePathChain,
    scanFolder,
    type FileBrowserState
  } from '../file-browser.svelte';
  import { type ControlBarItem } from '../-file-browser/desktop/main-panel/control-bar.svelte';
  import FilterOverlay, { filterOverlayState } from './arrange-overlay.svelte';

  import { page } from '$app/stores';
  import {
    Title,
    Awaiter,
    type AwaiterResetFunction,
    Banner,
    BannerClass,
    Button,
    LoadingSpinnerPage
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import NewDialog, { newDialogState } from './new-dialog.svelte';
  import { goto } from '$app/navigation';

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
      label: 'Arrange',
      icon: 'fa-solid fa-filter',
      action: async ({ currentTarget }) => {
        const bounds = (currentTarget as HTMLElement).getBoundingClientRect();

        $filterOverlayState.enabled = [window.innerWidth - bounds.right, bounds.bottom];
      },
      group: 'arrangement',
      isLoading: true
    },
    {
      label: 'New',
      icon: 'fa-solid fa-plus',
      action: (event) => {
        const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();

        $newDialogState = { x: bounds.left, y: bounds.bottom, state: { type: 'file', files: [] } };
      },
      isLoading: false,
      group: 'new'
    }
  ];

  const fileBrowserState: Writable<FileBrowserState> = writable({ isLoading: true, controlBarActions: [] });
  const error: Writable<Error | null> = writable(null);
</script>

{#key title}
  <Title title={title ?? 'My Files'} />
{/key}

<FilterOverlay onFilterApply={() => $refresh(true, null)} />

{#key id}
  <Awaiter
    bind:reset={$refresh}
    callback={async (): Promise<void> => {
      $fileBrowserState = { isLoading: true, controlBarActions: [] };

      try {
        const file = await getFile(id);

        const [files, pathChain, access] = await Promise.all([
          file.isFolder ? scanFolder(file, $filterOverlayState.state) : [],
          getFilePathChain(file),
          getFileAccessList(file),
        ])

        $fileBrowserState = {
          isLoading: false,

          files,
          pathChain,
          access,
          file,
          title: 'My Files',

          controlBarActions: actions
        }

        title = id != null ? $fileBrowserState.file?.name ?? null : null
      } catch (errorData: any) {
        $error = errorData
        throw errorData
      }
    }}
  >
    {#snippet error({ error })}
      <Banner bannerClass={BannerClass.Error}>
        <div class="error-banner">
          <p class="message">{error.name}: {error.message}</p>
          <Button onClick={() => $refresh(true)}>
            <p class="retry">Retry</p>
          </Button>
        </div>
      </Banner>
    {/snippet}
  </Awaiter>
{/key}

{#if $error == null}
  <FileBrowser {fileBrowserState} />
  <NewDialog
    {fileBrowserState}
    onNewFiles={() => {
      $refresh(true, null);
    }}
    onNewFolder={(id) => goto(`/app/files?id=${id}`)}
  />
{/if}

<style lang="scss">
  div.error-banner {
    height: 100%;

    flex-grow: 1;
    display: flex;
    flex-direction: column;
    justify-content: safe center;

    gap: 8px;
    font-weight: bolder;

    p.retry {
      margin: 8px;
    }
  }
</style>

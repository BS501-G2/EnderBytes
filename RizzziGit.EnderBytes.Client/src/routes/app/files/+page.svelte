<script lang="ts">
  import FileBrowser, {
    type FileBrowserState,
    type FileClipboard,
    fileClipboard
  } from '../file-browser.svelte';
  import { type ControlBarItem } from '../-file-browser/desktop/main-panel/control-bar.svelte';
  import FilterOverlay, { filterOverlayState } from './arrange-overlay.svelte';
  import { executeBackgroundTask } from '$lib/background-task.svelte';

  import { page } from '$app/stores';
  import {
    Title,
    Awaiter,
    type AwaiterResetFunction,
    Banner,
    BannerClass,
    Button
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import NewDialog, { newDialogState } from './new-dialog.svelte';
  import { goto } from '$app/navigation';
  import type { File } from '$lib/server/db/file';
  import { FileAccessLevel, FileType } from '$lib/shared/db';
  import {
    getAuthentication,
    getFile,
    listPathChain,
    scanFolder,
    listFileAccess,
    readFile,
    getFileMimeType,
    moveFile
  } from '$lib/client/api-functions';

  function parseId(id: string | null) {
    if (id == null) {
      return undefined;
    } else {
      return Number.parseInt(id) || undefined;
    }
  }

  const id = $derived(parseId($page.url.searchParams.get('id')));
  const selection: Writable<File[]> = writable([]);

  let refresh: Writable<AwaiterResetFunction<null>> = writable();
  let title: string | null = $state(null);

  const actions: ControlBarItem[] = [
    {
      label: 'Refresh',
      icon: 'fa-solid fa-sync',
      action: async () => {
        await $refresh(true, null);
      },
      group: 'arrangement',
      isVisible: () =>
        !$fileBrowserState.isLoading && $fileBrowserState.file?.type === FileType.Folder
    },
    {
      label: 'Arrange',
      icon: 'fa-solid fa-filter',
      action: async ({ currentTarget }) => {
        const bounds = (currentTarget as HTMLElement).getBoundingClientRect();

        $filterOverlayState.enabled = [window.innerWidth - bounds.right, bounds.bottom];
      },
      group: 'arrangement',
      isVisible: () =>
        !$fileBrowserState.isLoading && $fileBrowserState.file?.type === FileType.Folder
    },
    {
      label: 'Open File',
      icon: 'fa-solid fa-folder-open',
      action: async () => {
        await goto(`/app/files?id=${$selection[0].id}`);
      },
      group: 'actions',
      isVisible: (selection) =>
        !$fileBrowserState.isLoading &&
        window.matchMedia('(any-pointer: coarse)').matches &&
        selection.length === 1 &&
        selection[0].id != id
    },
    {
      label: 'Delete',
      icon: 'fa-solid fa-trash',
      action: async () => {},
      group: 'actions',
      isVisible: (selection) => !$fileBrowserState.isLoading && selection.length > 0
    },
    {
      label: 'Download',
      icon: 'fa-solid fa-download',
      action: async () => {
        const file = $selection[0];
        if (file == null) {
          return;
        }

        const ro = await readFile(file);
        const blob = new Blob([ro], { type: await getFileMimeType(file) });
        const url = URL.createObjectURL(blob);

        window.open(url, '_blank');
      },
      group: 'actions',
      isVisible: (selection) =>
        !$fileBrowserState.isLoading && selection.length == 1 && selection[0].type === FileType.File
    },
    {
      label: 'Cut',
      icon: 'fa-solid fa-scissors',
      action: async () => {
        if ($fileBrowserState.isLoading || $fileBrowserState.file == null) {
          return;
        }

        $fileClipboard = {
          files: $selection,
          ownerUserId: $fileBrowserState.file.ownerUserId,
          isCut: true
        };

        await executeBackgroundTask('Cut', false, (_, set) => {
          set(`${$selection.length} file(s) put to clipboard.`);
        }).run();
        $selection = [];
      },
      group: 'actions',
      isVisible: (selection) => !$fileBrowserState.isLoading && selection.length > 0
    },
    {
      label: 'Copy',
      icon: 'fa-solid fa-copy',
      action: async () => {
        if ($fileBrowserState.isLoading || $fileBrowserState.file == null) {
          return;
        }

        $fileClipboard = {
          files: $selection,
          ownerUserId: $fileBrowserState.file.ownerUserId,
          isCut: false
        };

        await executeBackgroundTask('Move', false, (_, set) => {
          set(`${$selection.length} file(s) put to clipboard.`);
        }).run();

        $selection = [];
      },
      group: 'actions',
      isVisible: (selection) => !$fileBrowserState.isLoading && selection.length > 0
    },
    {
      label: 'New',
      icon: 'fa-solid fa-plus',
      action: (event) => {
        const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();

        $newDialogState = { x: bounds.left, y: bounds.bottom, state: { type: 'file', files: [] } };
      },
      group: 'new',
      isVisible: () =>
        !$fileBrowserState.isLoading &&
        $fileBrowserState.file?.type === FileType.Folder &&
        ($fileBrowserState.access?.highestLevel ?? FileAccessLevel.Full) >= FileAccessLevel.Read
    },
    {
      label: 'Paste',
      icon: 'fa-solid fa-clipboard',
      action: async () => {
        await executeBackgroundTask('Paste', false, async (_, set) => {
          if ($fileBrowserState.isLoading || $fileBrowserState.file == null) {
            return;
          }

          if ($fileClipboard == null) {
            return;
          }

          if ($fileClipboard.isCut) {
            await moveFile($fileClipboard.files, $fileBrowserState.file);
          }

          set(`${$fileClipboard.files.length} file(s) pasted.`);
          $refresh?.(true, null);
        }).run();

        $fileClipboard = null;
      },
      group: 'actions',
      isVisible: () =>
        $fileClipboard != null &&
        !$fileBrowserState.isLoading &&
        $fileBrowserState.file?.type === FileType.Folder &&
        $selection.length === 0 &&
        $fileClipboard.ownerUserId === $fileBrowserState.file?.ownerUserId
    },
    {
      label: 'Cancel',
      icon: 'fa-solid fa-xmark',
      action: async () => {
        $fileClipboard = null;
      },
      group: 'actions',
      isVisible: () =>
        $fileClipboard != null &&
        !$fileBrowserState.isLoading &&
        $fileBrowserState.file?.type === FileType.Folder &&
        $selection.length === 0 &&
        $fileClipboard != null
    }
  ];

  const fileBrowserState: Writable<FileBrowserState> = writable({
    isLoading: true,
    controlBarActions: []
  });
  const errorStore: Writable<Error | null> = writable(null);
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
        const file = await getFile(id ?? null);

        const [files, pathChain, accesses] = await Promise.all([
          file.type === FileType.Folder ? scanFolder(file) : [],
          listPathChain(file),
          listFileAccess(file),
        ])

        await new Promise<void>((resolve) => setTimeout(resolve, 250))

        $fileBrowserState = {
          isLoading: false,

          files,
          pathChain: {
            root: pathChain[0],
            chain: pathChain.slice(1),
            isForeign: pathChain[0].ownerUserId === getAuthentication()!.userId
          },
          access: file.ownerUserId === getAuthentication()!.userId ? null : {
            highestLevel: accesses[0].accessLevel,
            accessPoint:  accesses[0],
            list: accesses
          },
          file,
          title: 'My Files',

          controlBarActions: actions
        }

        title = id != null ? $fileBrowserState.file?.name ?? null : null
      } catch (errorData: any) {
        $errorStore = errorData
        throw $errorStore
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

{#if $errorStore == null}
  <FileBrowser {fileBrowserState} {selection} />
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

<script lang="ts" context="module">
  export interface NewDialogState {
    x: number;
    y: number;

    state:
      | {
          type: 'file';
          files: File[];
        }
      | {
          type: 'folder';
          name: string;
        };
  }

  export const newDialogState: Writable<NewDialogState | null> = writable(null);
</script>

<script lang="ts">
  import {
    Button,
    ButtonClass,
    Dialog,
    Input,
    InputType,
    LoadingSpinner,
    Overlay,
    OverlayPositionType,
    ResponsiveLayout,
    ViewMode,
    viewMode
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { fly } from 'svelte/transition';
  import { type FileBrowserState } from '../file-browser.svelte';
  import { createFile, createFolder } from '$lib/client/api-functions';
  import type { File as DbFile } from '$lib/server/db/file';
  import { FileType } from '$lib/shared/db';

  const {
    fileBrowserState,
    onNewFolder,
    onNewFiles
  }: {
    fileBrowserState: Writable<FileBrowserState>;
    onNewFiles: (...ids: number[]) => void;
    onNewFolder: (id: number) => void;
  } = $props();

  const files: Writable<File[]> = writable([]);
  const fileInput: Writable<HTMLInputElement | null> = writable(null);

  function onDismiss() {
    $newDialogState = null;
  }

  const tabs: [type: 'file' | 'folder', name: string, icon: string, description: string][] = [
    ['file', 'File', 'fa-solid fa-file', 'Create a new file.'],
    ['folder', 'Folder', 'fa-solid fa-folder', 'Create a new folder.']
  ];

  const newFolderName: Writable<string> = writable('');
</script>

{#if $newDialogState != null}
  {@const { x, y } = $newDialogState}

  <ResponsiveLayout>
    {#snippet desktop()}
      <Overlay position={[OverlayPositionType.Offset, x - 2, y + 8]} {onDismiss}>
        <div class="new-overlay" transition:fly|global={{ duration: 200, y: -16 }}>
          {@render content($newDialogState!)}
        </div>
      </Overlay>
    {/snippet}

    {#snippet mobile()}
      <Dialog {onDismiss}>
        {#snippet head()}
          <h2>New</h2>
        {/snippet}
        {#snippet body()}
          {@render content($newDialogState!)}
        {/snippet}
      </Dialog>
    {/snippet}
  </ResponsiveLayout>
{/if}

{#snippet content({ state }: NewDialogState)}
  <div class="new-container">
    <div class="tab-host">
      {#each tabs as [type, name, icon, description]}
        <Button
          buttonClass={ButtonClass.Transparent}
          enabled={state.type != type}
          onClick={() => {
            if ($newDialogState === null) {
              return;
            }

            if (type == 'file') {
              $newDialogState.state = { type: 'file', files: [] };
            } else if (type == 'folder') {
              $newDialogState.state = { type: 'folder', name: '' };
            }

            $newDialogState = $newDialogState;
          }}
          outline={false}
          hint={description}
        >
          <div class="button {state.type == type ? 'active' : ''}">
            <i class={icon}></i>
            <p>{name}</p>
          </div>
        </Button>
      {/each}
    </div>
    <div class="tab-view">
      {#if state.type == 'file'}
        {@const onCreate = async (files: File[]) => {
          if ($fileBrowserState.isLoading) {
            return
          }
          const newFiles: DbFile[] = []

          for (const file of files) {
            const blob = new Blob([file], { type: file.type });
            const newFile = await createFile($fileBrowserState.file! as DbFile & { type: FileType.Folder }, file.name, new Uint8Array(await blob.arrayBuffer()));

            newFiles.push(newFile)
          }

          onNewFiles(...newFiles.map(file => file.id));
        }}

        <div class="input-group">
          <p class="description{$viewMode & ViewMode.Desktop ? ' desktop' : ''}">
            A new file will be created in the current folder. Alternatively files can be dragged
            inside the folder view to upload.
          </p>
          <input
            type="file"
            multiple
            hidden
            bind:this={$fileInput}
            onchange={({ currentTarget }) => {
              $files = Array.from(currentTarget?.files ?? []);
            }}
          />
          {#if $files.length > 0}
            <ul>
              <li>
                <b>Selected Files:</b>
              </li>
              {#each $files as file}
                <li>{file.name}</li>
              {/each}
            </ul>
          {/if}
          <Button buttonClass={ButtonClass.PrimaryContainer} onClick={() => $fileInput?.click()}>
            <p class="button">Select Files</p>
            {#snippet loading()}
              <p class="button"><LoadingSpinner size="1em" /></p>
            {/snippet}
            {#snippet error(error)}
              <p class="button">{error.message}</p>
            {/snippet}
          </Button>
          <Button
            buttonClass={ButtonClass.Primary}
            onClick={async () => {
              try {
                await onCreate($files);
              } finally {
                $files = [];
              }
            }}
          >
            <p class="button">Upload File(s)</p>
            {#snippet loading()}
              <p class="button"><LoadingSpinner size="1em" /></p>
            {/snippet}
            {#snippet error(error)}
              <p class="button">{error.message}</p>
            {/snippet}
          </Button>
        </div>
      {:else if state.type == 'folder'}
        {@const onCreate = async () => {
          if ($fileBrowserState.isLoading) {
            return
          }

          const folder = await createFolder($fileBrowserState.file! as DbFile & { type: FileType.Folder }, $newFolderName);
          onDismiss();
          onNewFolder(folder.id)
          $newFolderName = ''
        }}

        <div class="input-group">
          <p class="description{$viewMode & ViewMode.Desktop ? ' desktop' : ''}">
            A new folder will be created in the current folder.
          </p>
          <Input type={InputType.Text} name="Folder Name" value={newFolderName} />
          <Button buttonClass={ButtonClass.Primary} onClick={onCreate}>
            <p class="button">Create Folder</p>
            {#snippet loading()}
              <p class="button"><LoadingSpinner size="1em" /></p>
            {/snippet}
            {#snippet error(error)}
              <p class="button">{error.message}</p>
            {/snippet}
          </Button>
        </div>
      {/if}
    </div>
  </div>
{/snippet}

<style lang="scss">
  div.new-container {
    display: flex;
    flex-direction: column;
    gap: 8px;

    min-height: 0px;

    > div.tab-view {
      padding: 8px;
      min-height: 0px;

      div.input-group {
        display: flex;
        flex-direction: column;
        align-items: stretch;
        gap: 8px;

        min-height: 0px;

        > ul {
          overflow: auto;

          min-height: 0px;
          max-height: min(512px, 100vh - 512px);

          display: flex;
          flex-direction: column;
        }
      }

      p.button {
        margin: 8px;
      }

      p.description.desktop {
        max-width: 256px;
        text-align: justify;
      }
    }

    > div.tab-host {
      display: flex;
      flex-direction: row;
      gap: 8px;

      div.button {
        display: flex;
        flex-direction: row;
        align-items: center;

        gap: 8px;
        padding: 8px;
        border: 1px solid transparent;
      }

      div.button.active {
        border-bottom-color: var(--primary);
      }
    }
  }

  div.new-overlay {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    padding: 8px;
    border-radius: 8px;

    display: flex;
    flex-direction: column;

    min-height: 0px;

    box-shadow: var(--shadow) 0px 0px 8px;
  }
</style>

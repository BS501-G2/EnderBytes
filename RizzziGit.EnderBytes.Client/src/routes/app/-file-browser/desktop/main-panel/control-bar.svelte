<script lang="ts">
  import { Button, ButtonClass } from '@rizzzi/svelte-commons';
  import { scale } from 'svelte/transition';
  import type { FileBrowserState, FileResource } from '../../../file-browser.svelte';
  import UploadFileDialog from './control-bar/upload-file-dialog.svelte';
  import CreateFolderDialog from './control-bar/create-folder-dialog.svelte';

  let {
    fileBrowserState = $bindable(),
    selection = $bindable()
  }: { fileBrowserState: FileBrowserState; selection: FileResource[] } = $props();

  type ControlBarItemGroup = 'new' | 'actions' | 'arrangement';

  interface ControlBarItem {
    label: string;
    icon: string;
    group: ControlBarItemGroup;
    action: () => Promise<void>;
  }

  function getControlBarItems(): ControlBarItem[] {
    const items: ControlBarItem[] = [];

    items.push({
      label: 'Filter',
      icon: 'fa-solid fa-filter',
      group: 'arrangement',
      action: async () => await fileBrowserState.onFilter?.()
    });

    if (!fileBrowserState.isLoading) {
      if (fileBrowserState.file?.isFolder) {
        if (fileBrowserState.allowCreate && (fileBrowserState.access?.highestExtent ?? 0 > 2)) {
          items.push({
            label: 'Upload',
            icon: 'fa-solid fa-file-circle-plus',
            group: 'new',
            action: async () => {
              uploadFileDialog = true;
            }
          });

          items.push({
            label: 'New Folder',
            icon: 'fa-solid fa-folder-plus',
            group: 'new',
            action: async () => {
              createFolderDialog = true;
            }
          });
        }

        if (fileBrowserState.access?.highestExtent ?? 0 > 3) {
          if (selection.length > 0) {
            items.push({
              label: 'Trash',
              icon: 'fa-solid fa-trash',
              group: 'actions',
              action: async () => {}
            });
          }

          if (selection.length === 1) {
            items.push({
              label: 'Rename',
              icon: 'fa-solid fa-pencil',
              group: 'actions',
              action: async () => {}
            });

            items.push({
              label: 'Open',
              icon: 'fa-solid fa-folder-open',
              group: 'actions',
              action: async () => {}
            });
          }
        }
      }
    }

    return items;
  }

  const actions = $derived(getControlBarItems());

  let uploadFileDialog: boolean = $state(false);
  let createFolderDialog: boolean = $state(false);
</script>

{#if uploadFileDialog}
  <UploadFileDialog bind:enabled={uploadFileDialog} bind:fileBrowserState />
{/if}
{#if createFolderDialog}
  <CreateFolderDialog bind:enabled={createFolderDialog} bind:fileBrowserState />
{/if}

{#snippet buttons(actions: ControlBarItem[], animations: boolean)}
  {#each actions as action}
    {#snippet entry(action: ControlBarItem)}
      <i class="icon {action.icon}"></i>
      <p>{action.label}</p>
    {/snippet}

    <Button
      buttonClass={ButtonClass.BackgroundVariant}
      hint={action.label}
      outline={false}
      onClick={action.action}
    >
      {#if animations}
        <div class="button" transition:scale|global={{ duration: 200, start: 0.95 }}>
          {@render entry(action)}
        </div>
      {:else}
        <div class="button">{@render entry(action)}</div>
      {/if}
    </Button>
  {/each}
{/snippet}

<div class="control-bar-container">
  <div class="control-bar">
    {#snippet action(group: ControlBarItemGroup, animations: boolean)}
      {@const filteredActions = actions.filter((action) => action.group == group)}

      {#if filteredActions.length != 0}
        {#if animations}
          <div class="control-group" transition:scale|global={{ duration: 200, start: 0.95 }}>
            {@render buttons(filteredActions, animations)}
          </div>
        {:else}
          <div class="control-group">
            {@render buttons(filteredActions, animations)}
          </div>
        {/if}
      {/if}
    {/snippet}

    {@render action('new', true)}
    {@render action('actions', true)}
    <div class="spacer"></div>
    {@render action('arrangement', false)}
  </div>
</div>

<style lang="scss">
  div.control-bar-container {
    min-width: 0px;

    min-height: 2em;
    // max-height: 2em;

    overflow: auto hidden;

    user-select: none;

    box-sizing: border-box;

    display: flex;
    flex-direction: column;

    > div.control-bar {
      gap: 8px;

      display: flex;
      flex-direction: row;
      align-items: center;
      min-height: 2em;
      max-height: 2em;

      > div.spacer {
        flex-grow: 1;
      }

      > div.control-group {
        display: flex;
        flex-direction: row;
        align-items: center;

        background-color: var(--backgroundVariant);
        color: var(--onBackgroundVariant);
        border-radius: 8px;

        min-height: 2em;
        max-height: 2em;
        padding: 0px 2px;
      }
    }
  }

  div.button {
    display: flex;
    align-items: center;

    min-height: 2em;

    gap: 8px;
  }

  i.icon {
    min-height: 100%;
    max-height: 100%;

    font-size: 1.25em;
  }

  p {
    text-wrap: nowrap;
  }
</style>

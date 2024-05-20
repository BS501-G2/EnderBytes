<script lang="ts">
  import { type Writable, type Readable, derived, writable } from 'svelte/store';
  import type { FileBrowserState, FileResource } from '../../file-browser.svelte';
  import { Button, ButtonClass } from '@rizzzi/svelte-commons';

  let {
    fileBrowserState,
    selection
  }: { fileBrowserState: Writable<FileBrowserState>; selection: Writable<FileResource[]> } =
    $props();

  const selected: Readable<FileResource | null> = derived(
    [fileBrowserState, selection],
    ([fileBrowserState, selection]) =>
      selection.length === 1
        ? selection[0]
        : fileBrowserState.isLoading
          ? null
          : fileBrowserState.file
  );

  type Tab = [name: string, icon: string, description: string];
  const tabs: Tab[] = [
    ['Details', 'fa-solid fa-file-lines', 'View file details.'],
    ['History', 'fa-solid fa-clock', 'View file history.'],
    ['Access', 'fa-solid fa-lock', 'View file access.']
  ];

  const currentTab: Writable<Tab> = writable(tabs[0]);
</script>

{#snippet tabHost()}
  <div class="tab-host">
    {#each tabs as tab}
      <Button
        outline={false}
        onClick={() => {
          $currentTab = tab;
        }}
        buttonClass={ButtonClass.Transparent}
      >
        <div class="tab-button{tab[0] == $currentTab[0] ? ' active' : ''}">
          <i class="tab-icon {tab[1]}"></i>
          <p>{tab[0]}</p>
        </div>
      </Button>
    {/each}
  </div>
{/snippet}

{#snippet sidePanel(selected: FileResource | null, size: number)}
  <div class="header">
    <i class="icon fa-solid fa-{selected?.isFolder ? 'folder' : 'file'}"></i>
    <h3 class="file-name">
      {#if size > 1}
        {size} selected
      {:else if selected != null}
        {$fileBrowserState.title ?? (selected?.parentId != null ? selected?.name : 'My Files')}
      {/if}
    </h3>
  </div>
  {#if selected != null && size == 1}
    <div class="body">
      {@render tabHost()}
    </div>
  {/if}
{/snippet}

<div class="side-panel-container">
  {#if !$fileBrowserState.isLoading}
    {@render sidePanel($selected, $selection.length || 1)}
  {/if}
</div>

<style lang="scss">
  div.tab-host {
    display: flex;
    flex-direction: row;
    align-items: center;
    border-bottom: 2px solid transparent;

    justify-content: space-evenly;
  }

  div.tab-button {
    border-bottom: 2px solid transparent;
    padding: 8px;
  }

  div.tab-button.active {
    border-bottom: 2px solid var(--primary);
  }

  div.body {
    flex-grow: 1;
    min-height: 0px;
    min-width: 0px;

    overlay: auto;

    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  div.header {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    > h3.file-name {
      flex-grow: 1;

      overflow: hidden;
      text-overflow: ellipsis;
      text-wrap: nowrap;
    }

    > i.icon {
      min-width: 16px;
      max-width: 16px;

      text-align: center;
      aspect-ratio: 1;
    }
  }

  div.side-panel-container {
    border-radius: 8px;

    display: flex;
    flex-direction: column;
    gap: 8px;

    min-width: 320px;
    max-width: 320px;

    padding: 16px;
    box-sizing: border-box;

    background-color: var(--backgroundVariant);
  }
</style>

<script lang="ts">
  import { type Writable, type Readable, derived } from 'svelte/store';
  import type { FileBrowserState, FileResource } from '../../file-browser.svelte';
  import { Button } from '@rizzzi/svelte-commons';

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
</script>

{#snippet sidePanel(selected: FileResource)}
  <div class="header">
    <i class="icon fa-solid fa-{selected?.isFolder ? 'folder' : 'file'}"></i>
    <h3>
      {$fileBrowserState.title ?? (selected?.parentId != null ? selected?.name : 'My Files')}
    </h3>
  </div>
{/snippet}

<div class="side-panel-container">
  {#if !$fileBrowserState.isLoading}
    {#if $selection.length > 1}
      <h2>Multiple files selected</h2>
    {:else if $selected == null}
      <h2>No file selected</h2>
    {:else}
      {@render sidePanel($selected)}
    {/if}
  {/if}
</div>

<style lang="scss">
  div.header {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    > h3 {
      flex-grow: 1;
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

    min-width: 320px;
    max-width: 320px;

    padding: 16px;
    box-sizing: border-box;

    background-color: var(--backgroundVariant);
  }
</style>

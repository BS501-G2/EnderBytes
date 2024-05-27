<script lang="ts">
  import { type Writable, type Readable, derived } from 'svelte/store';
  import type { FileBrowserState } from '../../file-browser.svelte';
  import FileDetails from './main-panel/side-panel/file-details.svelte';
  import type { FileResource } from '$lib/client/file';

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

<div class="side-panel-container">
  {#if !$fileBrowserState.isLoading}
    <div class="header">
      <i class="icon fa-solid fa-{$selected?.isFolder ? 'folder' : 'file'}"></i>
      <h3 class="file-name">
        {#if $selection.length > 1}
          {$selection.length} selected
        {:else if $selected != null && $selected.parentId != null}
          {$selected.name}
        {:else if $fileBrowserState.title != null}
          {$fileBrowserState.title}
        {/if}
      </h3>
    </div>
    <div class="body">
      {#if $selected != null && $selection.length === 1}
        <FileDetails file={$selected} />
      {/if}
    </div>
  {/if}
</div>

<style lang="scss">
  div.body {
    flex-grow: 1;
    min-height: 0px;
    min-width: 0px;

    display: flex;
    flex-direction: column;

    gap: 8px;
    padding: 8px;
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

      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
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

<script lang="ts">
  import type { Writable } from 'svelte/store';
  import type { FileBrowserState } from '../../file-browser.svelte';
  import ControlBar from './main-panel/control-bar.svelte';
  import FileList from './main-panel/file-list.svelte';
  import FileView from './main-panel/file-view.svelte';
  import PathChain from './main-panel/path-chain.svelte';
  import type { File } from '$lib/server/db/file';
  import { FileType } from '$lib/shared/db';

  let {
    fileBrowserState,
    selection
  }: { fileBrowserState: Writable<FileBrowserState>; selection: Writable<File[]> } = $props();
</script>

<div class="main-panel">
  {#if !$fileBrowserState.hidePathChain && ($fileBrowserState.isLoading || $fileBrowserState?.pathChain != null)}
    <PathChain fileBrowserState={fileBrowserState as any} />
  {/if}

  {#if $fileBrowserState.isLoading || ($fileBrowserState.file != null && $fileBrowserState.file.type === FileType.Folder)}
    <ControlBar fileBrowserState={fileBrowserState as any} {selection} />
  {/if}

  <div class="inner-panel">
    {#if !$fileBrowserState.isLoading && $fileBrowserState.file != null && $fileBrowserState.file.type !== FileType.Folder}
      <FileView fileBrowserState={fileBrowserState as any} {selection} />
    {:else}
      <FileList fileBrowserState={fileBrowserState as any} {selection} />
    {/if}
  </div>
</div>

<style lang="scss">
  div.main-panel {
    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-width: 0px;
    min-height: 0px;

    gap: 16px;

    > div.inner-panel {
      flex-grow: 1;
      min-height: 0px;
      min-width: 0px;

      display: flex;
      flex-direction: column-reverse;
    }
  }
</style>

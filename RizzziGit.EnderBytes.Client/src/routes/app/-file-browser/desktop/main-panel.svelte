<script lang="ts">
  import type { FileBrowserState, FileResource } from '../../file-browser.svelte';
  import ControlBar from './main-panel/control-bar.svelte';
  import FileList from './main-panel/file-list.svelte';
  import PathChain from './main-panel/path-chain.svelte';

  let {
    fileBrowserState = $bindable(),
    selection = $bindable()
  }: { fileBrowserState: FileBrowserState; selection: FileResource[] } = $props();
</script>

<div class="main-panel">
  {#if !fileBrowserState.hidePathChain && (fileBrowserState.isLoading || fileBrowserState?.pathChain != null)}
    <PathChain {fileBrowserState} />
  {/if}

  <ControlBar {fileBrowserState} bind:selection />

  {#if fileBrowserState.isLoading || fileBrowserState.file == null || fileBrowserState.file.isFolder}
    <FileList {fileBrowserState} bind:selection />
  {/if}
</div>

<style lang="scss">
  div.main-panel {
    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-width: 0px;
    min-height: 0px;

    gap: 16px;
  }
</style>

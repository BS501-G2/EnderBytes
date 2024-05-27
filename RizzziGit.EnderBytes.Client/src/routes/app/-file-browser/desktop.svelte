<script lang="ts">
  import type { Writable } from 'svelte/store';
  import type { FileBrowserState } from '../file-browser.svelte';
  import MainPanel from './desktop/main-panel.svelte';
  import SidePanel from './desktop/side-panel.svelte';
  import type { FileResource } from '$lib/client/file';

  const {
    fileBrowserState,
    selection
  }: { fileBrowserState: Writable<FileBrowserState>; selection: Writable<FileResource[]> } =
    $props();
</script>

<div>
  <MainPanel fileBrowserState={fileBrowserState as any} {selection} />

  {#if !$fileBrowserState.hideSidePanel}
    <SidePanel fileBrowserState={fileBrowserState as any} {selection} />
  {/if}
</div>

<style lang="scss">
  div {
    display: flex;
    flex-direction: row;

    flex-grow: 1;
    min-height: 100%;
    box-sizing: border-box;

    padding: 16px;
    gap: 16px;
  }
</style>

<script lang="ts">
  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";
  import { type AwaiterResetFunction } from "../../Bindings/Awaiter.svelte";
  import FileDetails from "./FileArea/FileDetails.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../../FileBrowser.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="file-area">
  {#if info == null}
    <FileList {selection} info={null} />
    <FileDetails {selection} />
  {:else}
    {@const file = info.current}
    {#if file.type == 0}
      <FileView {selection} {reset} {info} />
    {:else if file.type == 1}
      <FileList {selection} {info} />
      <FileDetails {selection} />
    {/if}
  {/if}
</div>

<style lang="scss">
  div.file-area {
    display: flex;

    flex-grow: 1;
    min-height: 0px;

    user-select: none;

    gap: 16px;
  }
</style>

<script lang="ts">
  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";
  import { type AwaiterResetFunction } from "../../Bindings/Awaiter.svelte";
  import FileDetails from "./FileArea/FileDetails.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../../FileBrowser.svelte";
  import ControlBar from "./ControlBar.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="file-area">
  {#if info == null}
    <div class="column">
      <ControlBar {selection} {reset} {info} />
      <FileList {selection} {info} />
    </div>
    <FileDetails {selection} />
  {:else}
    {@const file = info.current}
    {#if file.type == 0}
      <div class="column">
        <ControlBar {selection} {reset} {info} />
        <FileView {selection} {reset} {info} />
      </div>
    {:else if file.type == 1}
      <div class="column">
        <ControlBar {selection} {reset} {info} />
        <FileList {selection} {info} />
      </div>
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

    > div.column {
      flex-grow: 1;

      gap: 16px;

      min-height: 0px;
      overflow: hidden;

      display: flex;
      flex-direction: column;
    }
  }
</style>

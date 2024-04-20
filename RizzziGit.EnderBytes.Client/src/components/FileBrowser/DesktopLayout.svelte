<script lang="ts">
  import type { AwaiterResetFunction } from "../Bindings/Awaiter.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../FileBrowser.svelte";

  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="content">
  <ControlBar {selection} {reset} {info} />
  <div class="bezel">
    <AddressBar {info} />
    <FileArea {selection} {reset} {info} />
  </div>
</div>

<style lang="scss">
  div.content {
    background-color: var(--primaryContainer);
    color: var(--onPrimaryContainer);

    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;

    > div.bezel {
      flex-grow: 1;

      display: flex;
      flex-direction: column;

      padding: 16px;
      box-sizing: border-box;

      gap: 16px;

      border-radius: 16px 0px 0px 0px;
      overflow: hidden;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>

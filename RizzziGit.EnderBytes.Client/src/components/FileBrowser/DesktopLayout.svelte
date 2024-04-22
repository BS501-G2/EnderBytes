<script lang="ts">
  import type { AwaiterResetFunction } from "../Bindings/Awaiter.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../FileBrowser.svelte";

  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="container">
  <div class="bezel">
    <div class="content">
      <AddressBar {info} />
      <FileArea {selection} {reset} {info} />
    </div>
  </div>
</div>

<style lang="scss">
  div.container {
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

      min-height: 0px;

      border-radius: 16px;

      background-color: var(--background);
      color: var(--onBackground);

      > div.content {
        display: flex;
        flex-direction: column;

        gap: 16px;

        flex-grow: 1;

        overflow: auto;
      }
    }
  }
</style>

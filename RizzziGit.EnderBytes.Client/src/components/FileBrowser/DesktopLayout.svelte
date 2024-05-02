<script lang="ts">
  import type { AwaiterResetFunction } from "../Bindings/Awaiter.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../FileBrowser.svelte";
  import {
    runningBackgroundTasks,
    failedBackgroundTasks,
  } from "../BackgroundTaskList.svelte";
  import { enabled as operationsMenu } from "../Dashboard/DesktopLayout/TitleBar/Miscellaneous/OperationButton.svelte";

  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";
  import { session } from "../Bindings/Client.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="content">
  <AddressBar {info} />
  <FileArea {selection} {reset} {info} />
</div>

<svelte:window
  on:beforeunload={(event) => {
    if (
      ($runningBackgroundTasks.length > 0 ||
        $failedBackgroundTasks.length > 0) &&
      $session != null
    ) {
      $operationsMenu = true;
      event.preventDefault();
    }
  }}
/>

<style lang="scss">
  div.content {
    background-color: var(--background);
    color: var(--onBackground);

    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;

    border-radius: 1em;
    flex-grow: 1;

    display: flex;
    flex-direction: column;

    padding: 16px;
    box-sizing: border-box;

    gap: 16px;

    min-height: 0px;
  }
</style>

<script lang="ts">
  import type { Writable } from "svelte/store";
  import type { FileBrowserState } from "../FileBrowser.svelte";

  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let fileBrowserState: Writable<FileBrowserState>;
</script>

<div class="container">
  <AddressBar {fileBrowserState} />
  <ControlBar {fileBrowserState} />
  <FileArea {fileBrowserState} />
</div>

<style lang="scss">
  div.container {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
  }
</style>

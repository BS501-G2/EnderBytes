<script lang="ts">
    import FileViewer from "../DesktopLayout/FileViewer.svelte";
import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";

  export let currentId: number | null;
</script>

<div class="container">
  <AddressBar/>
  <ControlBar />
  <FileViewer/>
</div>

<style lang="scss">
  div.container {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
  }
</style>

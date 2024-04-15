<script lang="ts">
  import AddressBar from "./DesktopLayout/AddressBar/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let currentFileId: number | null;

  let selectedFileIds: number[] = [];
  let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;

  $: {
    currentFileId;

    selectedFileIds = [];
  }
</script>

<div class="content">
  <AddressBar bind:currentFileId />
  <ControlBar bind:currentFileId bind:selectedFileIds bind:onRefresh />
  <FileArea bind:onRefresh bind:currentFileId bind:selectedFileIds />
</div>

<style lang="scss">
  div.content {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
  }
</style>

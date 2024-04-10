<script lang="ts">
  import { onNavigate } from "$app/navigation";
  import type { Client } from "$lib/client/client";
  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let currentFileId: number | null;
  export let client: Client;
  export let fileCreationDialog: boolean;

  let selectedFileIds: number[] = [];
  let onRefresh: () => void

  $: {
    currentFileId;
    selectedFileIds = [];
  }
</script>

<div class="container">
  <AddressBar {client} bind:currentFileId />
  <ControlBar
    {client}
    bind:currentFileId
    bind:fileCreationDialog
    bind:selectedFileIds
    bind:onRefresh
  />
  <FileArea {client} bind:onRefresh bind:currentFileId bind:selectedFileIds />
</div>

<style lang="scss">
  div.container {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
  }
</style>

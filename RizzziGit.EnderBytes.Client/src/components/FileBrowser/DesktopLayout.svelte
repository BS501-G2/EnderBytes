<script lang="ts">
  import type { Client } from "$lib/client/client";
  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let currentFileId: number | null;
  export let client: Client;
  export let fileCreationDialog: boolean

  let selectedFileIds: number[] = [];

  $: console.log(fileCreationDialog)
</script>

<div class="container">
  <AddressBar {client} {currentFileId} />
  <ControlBar {client} {currentFileId} bind:fileCreationDialog bind:selectedFileIds />
  <FileArea {client} {currentFileId} bind:selectedFileIds />
</div>

<style lang="scss">
  div.container {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
  }
</style>

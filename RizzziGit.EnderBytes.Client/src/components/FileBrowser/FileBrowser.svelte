<script lang="ts">
  import DesktopLayout from "./DesktopLayout.svelte";
  import type { Client } from "$lib/client/client";
  import FileCreationDialog from "./FileCreationDialog/FileCreationDialog.svelte";
  import ResponsiveLayout from "../Bindings/ResponsiveLayout.svelte";

  export let client: Client;
  export let currentFileId: number | null;

  let fileCreationDialog: boolean = false;
</script>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <DesktopLayout {client} bind:currentFileId bind:fileCreationDialog />
  </svelte:fragment>
</ResponsiveLayout>

{#if fileCreationDialog}
  <FileCreationDialog
    bind:folderId={currentFileId}
    {client}
    onDismiss={() => (fileCreationDialog = false)}
  />
{/if}

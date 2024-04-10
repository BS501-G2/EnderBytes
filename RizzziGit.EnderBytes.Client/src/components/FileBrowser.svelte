<script lang="ts" context="module">
  import { ViewMode } from "$lib/view-mode";
</script>

<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import DesktopLayout from "./FileBrowser/DesktopLayout.svelte";
  import type { Client } from "$lib/client/client";
  import FileCreationDialog from "./FileBrowser/FileCreationDialog.svelte";

  export let client: Client;
  export let currentFileId: number | null;

  let fileCreationDialog: boolean = false;

  const rootState = RootState.state;
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <DesktopLayout {client} {currentFileId} bind:fileCreationDialog />
{/if}

{#if fileCreationDialog}
  <FileCreationDialog {client} onDismiss={() => (fileCreationDialog = false)} />
{/if}

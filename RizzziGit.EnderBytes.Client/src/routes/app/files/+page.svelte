<script lang="ts">
  import { onMount } from 'svelte'

  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  import DesktopLayout from "./DesktopLayout.svelte";

  const rootState = RootState.state;
  const appState = $rootState.appState
  const fileState = $appState.fileState

  export let currentId: number | null = null;

  onMount(() => {
    $fileState.currentFileId = currentId
  })
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <DesktopLayout {currentId} />
{/if}

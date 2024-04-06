<script lang="ts">
  import { onMount } from 'svelte'
  import { writable } from 'svelte/store'

  import { RootState } from "$lib/states/root-state";

  import FileBrowser, { NormalFileBrowserState } from '../../../components/FileBrowser.svelte'

  const rootState = RootState.state;
  const appState = $rootState.appState
  const fileState = $appState.fileState

  export let initialId: number | null = null;

  const fileBrowserState = writable(new NormalFileBrowserState(initialId))

  onMount(() => {
    $fileState.currentFileId = initialId
  })
</script>

<FileBrowser {fileBrowserState} />

<script lang="ts">
  import { Title } from "@rizzzi/svelte-commons";
  import FileBrowser, { type FileBrowserState } from '../file-browser.svelte';

  const fileBrowserState: FileBrowserState = {
    isLoading: false,

    files: [],
    pathChain: null,
    access: null,
    file: null,
    title: 'Starred'
  };
</script>

<Title title="Starred" />

<FileBrowser {fileBrowserState} />

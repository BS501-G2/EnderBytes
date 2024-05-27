<script lang="ts">
  import { goto } from '$app/navigation';
  import type { Writable } from 'svelte/store';
  import type { FileBrowserState } from '../../../../file-browser.svelte';
  import type { FileResource } from '$lib/client/file';

  let {
    file,
    fileBrowserState,
    onClick,
    selection
  }: {
    fileBrowserState: Writable<FileBrowserState & { isLoading: false }>;
    file: FileResource;
    onClick: (
      fileBrowserState: FileBrowserState & { isLoading: false },
      file: FileResource
    ) => void;
    selection: Writable<FileResource[]>;
  } = $props();
</script>

<script lang="ts">
  import { goto } from '$app/navigation';
  import type { Writable } from 'svelte/store';
  import type { FileResource, FileBrowserState } from '../../../../file-browser.svelte';

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

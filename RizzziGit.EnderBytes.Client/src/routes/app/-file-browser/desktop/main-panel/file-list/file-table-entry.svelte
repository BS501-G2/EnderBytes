<script lang="ts">
  import type { Writable } from 'svelte/store';
  import type { FileBrowserState } from '../../../../file-browser.svelte';
    import type { File } from '$lib/server/db/file';

  let {
    file,
    fileBrowserState,
    onClick,
    selection
  }: {
    fileBrowserState: Writable<FileBrowserState & { isLoading: false }>;
    file: File;
    onClick: (
      fileBrowserState: FileBrowserState & { isLoading: false },
      file: File
    ) => void;
    selection: Writable<File[]>;
  } = $props();
</script>

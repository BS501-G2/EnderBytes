<script lang="ts" context="module">
  import { ResponsiveLayout } from '@rizzzi/svelte-commons';
  import type { ControlBarItem } from './-file-browser/desktop/main-panel/control-bar.svelte';

  export type FileBrowserState = {
    title?: string;

    hidePathChain?: boolean;
    hideSidePanel?: boolean;

    controlBarActions?: ControlBarItem[];
  } & (
    | {
        isLoading: true;
      }
    | {
        isLoading: false;

        file: FileResource | null;
        access: FileAccessListInfo | null;
        pathChain: FilePathChainInfo | null;
        files: FileResource[];
      }
  );
</script>

<script lang="ts">
  import DesktopLayout from './-file-browser/desktop.svelte';
  import MobileLayout from './-file-browser/mobile.svelte';
  import { writable, type Writable } from 'svelte/store';
  import type { FileAccessListInfo, FilePathChainInfo, FileResource } from '$lib/client/file';

  const {
    fileBrowserState,
    selection = writable([])
  }: { fileBrowserState: Writable<FileBrowserState>; selection?: Writable<FileResource[]> } =
    $props();
</script>

<ResponsiveLayout>
  {#snippet desktop()}
    <DesktopLayout fileBrowserState={fileBrowserState as any} {selection} />
  {/snippet}
  {#snippet mobile()}
    <MobileLayout fileBrowserState={fileBrowserState as any} />
  {/snippet}
</ResponsiveLayout>

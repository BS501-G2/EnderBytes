<script lang="ts" context="module">
  import { ResponsiveLayout } from '@rizzzi/svelte-commons';
  import type { ControlBarItem } from './-file-browser/desktop/main-panel/control-bar.svelte';

  export interface FileAccessListInfo {
    highestLevel: FileAccessLevel;
    accessPoint: FileAccess;
    list: FileAccess[];
  }

  export interface FilePathChainInfo {
    root: File;
    chain: File[];

    isForeign: boolean;
  }

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

        file: File | null;
        access: FileAccessListInfo | null;
        pathChain: FilePathChainInfo | null;
        files: File[];
      }
  );
</script>

<script lang="ts">
  import DesktopLayout from './-file-browser/desktop.svelte';
  import MobileLayout from './-file-browser/mobile.svelte';
  import { writable, type Writable } from 'svelte/store';
  import type { File } from '$lib/server/db/file';
  import type { FileAccessLevel } from '$lib/shared/db';
  import type { FileAccess } from '$lib/server/db/file-access';

  const {
    fileBrowserState,
    selection = writable([])
  }: { fileBrowserState: Writable<FileBrowserState>; selection?: Writable<File[]> } =
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

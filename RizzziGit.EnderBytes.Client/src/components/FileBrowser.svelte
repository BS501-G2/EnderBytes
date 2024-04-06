<script lang="ts" context="module">
  import type { AppState } from "$lib/states/app-state";
  import { ViewMode } from "$lib/view-mode";

  export abstract class FileBrowserState {
    public constructor(initialFileId: number | null) {
      this.fileId = initialFileId;
      this.selectedFileIds = [];
    }

    public fileId: number | null;
    public selectedFileIds: number[];
  }

  export class NormalFileBrowserState extends FileBrowserState {
    public constructor(initialFileId: number | null) {
      super(initialFileId);
    }
  }
</script>

<script lang="ts">
  import type { Writable } from "svelte/store";

  import { RootState } from "$lib/states/root-state";

  import DesktopLayout from "./FileBrowser/DesktopLayout.svelte";

  export let fileBrowserState: Writable<FileBrowserState>;

  const rootState = RootState.state;
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <DesktopLayout {fileBrowserState} />
{/if}

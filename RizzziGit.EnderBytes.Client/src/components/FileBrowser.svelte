<script lang="ts" context="module">
  export interface BaseFileBrowserInformation {
    type: 0 | 1;
    current: any;
    pathChain: { isSharePoint: boolean; root: any; chain: any[] };
  }

  export interface FileBrowserFolderInformation
    extends BaseFileBrowserInformation {
    type: 1;
    files: any[];
  }

  export interface FileBrowserFileInformation
    extends BaseFileBrowserInformation {
    type: 0;
  }

  export type FileBrowserInformation =
    | FileBrowserFolderInformation
    | FileBrowserFileInformation;

  export type FileBrowserSelection = Writable<any[]>;
</script>

<script lang="ts">
  import DesktopLayout from "./FileBrowser/DesktopLayout.svelte";
  import ResponsiveLayout from "./Bindings/ResponsiveLayout.svelte";
  import FolderCreationDialog from "./FileBrowser/FolderCreationDialog.svelte";
  import FileCreationDialog from "./FileBrowser/FileCreationDialog.svelte";
  import { writable, type Writable } from "svelte/store";
  import Awaiter, {
    type AwaiterResetFunction,
  } from "./Bindings/Awaiter.svelte";
  import { apiFetch } from "./Bindings/Client.svelte";

  export let currentFileId: number | null;

  const selection: FileBrowserSelection = writable([]);

  async function load(): Promise<FileBrowserInformation> {
    // await new Promise<void>((resolve) => setTimeout(resolve, 1000));
    // throw new Error();

    const id = currentFileId != null ? `:${currentFileId}` : "!root";
    const [current, pathChain] = await Promise.all([apiFetch(`/file/${id}`), apiFetch(`/file/${id}/path-chain`)]);

    if (current.type == 1) {
      const files = await apiFetch(`/file/${id}/files`);

      return { type: 1, current, pathChain, files };
    } else {
      return { type: 0, current, pathChain };
    }
  }

  let reset: AwaiterResetFunction;

  $: {
    currentFileId;

    $selection = [];
  }
</script>

{#key currentFileId}
  <Awaiter callback={load} bind:reset>
    <svelte:fragment slot="loading">
      <ResponsiveLayout>
        <svelte:fragment slot="desktop">
          <DesktopLayout info={null} {reset} {selection} />
        </svelte:fragment>
      </ResponsiveLayout>
    </svelte:fragment>
    <svelte:fragment slot="success" let:result={info}>
      <ResponsiveLayout>
        <svelte:fragment slot="desktop">
          <DesktopLayout {info} {reset} {selection} />
        </svelte:fragment>
      </ResponsiveLayout>

      <FolderCreationDialog bind:currentFileId />
      <FileCreationDialog bind:currentFileId />
    </svelte:fragment>
  </Awaiter>
{/key}

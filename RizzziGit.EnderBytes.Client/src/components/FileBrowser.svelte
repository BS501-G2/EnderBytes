<script lang="ts" context="module">
  export interface FileBrowserInformation {
    current: any;

    pathChain: { isSharePoint: boolean; root: any; chain: any[] };
    files: any[];
  }

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
  import { fetchAndInterpret } from "./Bindings/Client.svelte";

  export let currentFileId: number | null;

  const selection: FileBrowserSelection = writable([]);

  async function load(): Promise<FileBrowserInformation> {
    // await new Promise<void>((resolve) => setTimeout(resolve, 1000));
    // throw new Error();

    const id = currentFileId != null ? `:${currentFileId}` : "!root";
    const [current, files, pathChain] = await Promise.all([
      fetchAndInterpret(`/file/${id}`),
      fetchAndInterpret(`/file/${id}/files`),
      fetchAndInterpret(`/file/${id}/path-chain`),
    ]);

    return { current, pathChain, files };
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

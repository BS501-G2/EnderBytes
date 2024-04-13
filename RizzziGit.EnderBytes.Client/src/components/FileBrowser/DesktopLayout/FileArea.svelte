<script lang="ts">
  import type { Client } from "$lib/client/client";

  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";
  import Awaiter from "../../Bindings/Awaiter.svelte";

  export let client: Client;
  export let currentFileId: number | null;
  export let selectedFileIds: number[] = [];
  export let onRefresh: () => void;

  async function getFile(): Promise<any> {
    return await client.getFile(
      currentFileId ?? (await client.getRootFolderId()),
    );
  }
</script>

{#key currentFileId}
  <div class="file-area">
    <Awaiter callback={getFile}>
      <svelte:fragment slot="success" let:result={file}>
        {#if file == null}
          <p>error</p>
        {:else if file.Type == 0}
          <FileView bind:client {file} bind:selectedFileIds />
        {:else if file.Type == 1}
          <FileList bind:client {file} bind:onRefresh bind:selectedFileIds />
        {/if}
      </svelte:fragment>
    </Awaiter>
  </div>
{/key}

<style lang="scss">
  div.file-area {
    display: flex;

    flex-grow: 1;
    min-height: 0px;

    user-select: none;
  }
</style>

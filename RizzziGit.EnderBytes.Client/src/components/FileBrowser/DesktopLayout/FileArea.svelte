<script lang="ts">
  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";
  import Awaiter from "../../Bindings/Awaiter.svelte";
  import ClientAwaiter from "../../Bindings/ClientAwaiter.svelte";

  export let currentFileId: number | null;
  export let selectedFileIds: number[] = [];
  export let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;
</script>

{#key currentFileId}
  <div class="file-area">
    <ClientAwaiter let:client>
      <Awaiter
        callback={async () => {
          const fileId = currentFileId ?? (await client.getRootFolderId());
          const result = await client.getFile(fileId);
          return result;
        }}
      >
        <svelte:fragment slot="success" let:result={file}>
          {#if file == null}
            <p>error</p>
          {:else if file.Type == 0}
            <FileView {file} bind:selectedFileIds />
          {:else if file.Type == 1}
            <FileList {file} bind:onRefresh bind:selectedFileIds />
          {/if}
        </svelte:fragment>
      </Awaiter>
    </ClientAwaiter>
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

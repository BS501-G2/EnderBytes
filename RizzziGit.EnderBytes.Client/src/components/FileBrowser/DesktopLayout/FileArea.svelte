<script lang="ts">
  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";
  import Awaiter from "../../Bindings/Awaiter.svelte";
  import { fetchAndInterpret } from "../../Bindings/Client.svelte";

  export let currentFileId: number | null;
  export let selectedFiles: number[] = [];
  export let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;
</script>

{#key currentFileId}
  <div class="file-area">
    <Awaiter
      callback={() =>
        fetchAndInterpret(
          `/file/${currentFileId != null ? `:${currentFileId}` : "!root"}`,
        )}
    >
      <svelte:fragment slot="success" let:result={file}>
        {#if file == null}
          <p>error</p>
        {:else if file.type == 0}
          <FileView {file} bind:selectedFiles />
        {:else if file.type == 1}
          <FileList {file} bind:onRefresh bind:selectedFiles />
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

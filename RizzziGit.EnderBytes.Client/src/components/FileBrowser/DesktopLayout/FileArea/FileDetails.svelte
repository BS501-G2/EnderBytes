<script lang="ts">
  import { FileIcon } from "svelte-feather-icons";

  export let selectedFileIds: number[];
</script>

<div class="file-details">
  <div class="file-preview">
    {#if selectedFileIds.length > 1}
      <FileIcon />
    {:else}
      <img alt="File preview for `file`" src="/favicon.svg" />
    {/if}
  </div>
  <div class="file-info">
    {#if selectedFileIds.length > 1}
      <p>{selectedFileIds.length} files</p>
    {:else}
      <span class="file-name">File name</span>
      <span class="file-size">File size</span>
    {/if}
  </div>
</div>

<style lang="scss">
  div.file-details {
    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-width: 320px;
    max-width: 320px;

    padding: 16px;

    overflow-y: auto;

    align-items: center;

    > div.file-preview {
      display: flex;
      flex-direction: column;

      width: 256px;
      height: 256px;

      align-items: center;

      margin: 16px;

      > img {
        width: 100%;
        height: 100%;
      }
    }
  }
</style>

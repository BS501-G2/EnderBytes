<script lang="ts">
  import { type Writable } from "svelte/store";

  import File from "../File.svelte";

  import { FileBrowserState } from "../../FileBrowser.svelte";

  export let fileBrowserState: Writable<FileBrowserState>;

  let files: string[] = ["test", "test2", "kasl alsd alskjcalsca sdk asd"];

  files = files.concat(files);
  files = files.concat(files);
  files = files.concat(files);
  files = files.concat(files);
</script>

<div class="file-area">
  <div class="file-list">
    {#each files as file}
      <File name={file} selected={false} />
    {/each}
  </div>

  <div class="divider"></div>

  <div class="file-details"></div>
</div>

<style lang="scss">
  div.file-area {
    display: flex;

    flex-grow: 1;
    min-height: 0px;

    > div.file-list {
      flex-grow: 1;

      padding: 16px;

      display: flex;
      flex-wrap: wrap;
      align-content: flex-start;
      justify-content: start;
      gap: 8px;

      overflow: auto;
      min-height: 0px;
    }

    > div.divider {
      min-width: 1px;
      background-color: var(--primaryContainer);
    }

    > div.file-details {
      min-width: 320px;
      flex-grow: 1;

      padding: 16px;
    }
  }
</style>

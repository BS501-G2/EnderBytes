<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import { onMount } from "svelte";
  import LoadingPage from "../../LoadingPage.svelte";
  import FileList from "./FileArea/FileList.svelte";
  import type { Client } from "$lib/client/client";

  export let client: Client;
  export let currentFileId: number | null;
  export let selectedFileIds: number[] = [];

  let loadPromise: Promise<any> | null = null;
  async function load(): Promise<any> {
    return await client.getFile(currentFileId ?? await client.getRootFolderId());
  }

  onMount(() => (loadPromise = load()));
</script>

<div class="file-area">
  {#if loadPromise == null}
    <LoadingPage />
  {:else}
    {#await loadPromise}
      <LoadingPage />
    {:then file}
      {#if file == null}
        <p>error</p>
      {:else if file.Type == 0}
        <p>a</p>
      {:else if file.Type == 1}
        <FileList {client} {file} bind:selectedFileIds />
      {/if}
    {/await}
  {/if}
</div>

<style lang="scss">
  div.file-area {
    display: flex;

    flex-grow: 1;
    min-height: 0px;

    user-select: none;
  }
</style>

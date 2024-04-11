<script lang="ts">
  import { onMount } from "svelte";
  import type { Client } from "$lib/client/client";

  import LoadingPage from "../../LoadingSpinnerPage.svelte";
  import FileList from "./FileArea/FileList.svelte";
  import FileView from "./FileArea/FileView.svelte";

  export let client: Client;
  export let currentFileId: number | null;
  export let selectedFileIds: number[] = [];
  export let onRefresh: () => void;

  let loadPromise: Promise<any> | null = null;
  async function load(): Promise<any> {
    return await client.getFile(
      currentFileId ?? (await client.getRootFolderId()),
    );
  }

  onMount(() => (loadPromise = load()));
  $: {
    currentFileId;
    loadPromise = load();
  }
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
        <FileView bind:client {file} bind:selectedFileIds />
      {:else if file.Type == 1}
        <FileList bind:client {file} bind:onRefresh bind:selectedFileIds />
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

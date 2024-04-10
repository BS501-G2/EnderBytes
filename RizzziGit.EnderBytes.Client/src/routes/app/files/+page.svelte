<script lang="ts">
  import { page } from "$app/stores";
  import { onMount } from "svelte";

  import FileBrowser from "../../../components/FileBrowser.svelte";
  import { RootState } from "$lib/states/root-state";

  const rootState = RootState.state;

  let currentFileId: number | null | undefined = undefined;

  onMount(() => {
    const currentId = $page.url.pathname.split("/")[3] ?? null;

    currentFileId =
      (currentId != null ? Number.parseInt(currentId) : null) ?? null;
  });
</script>

{#if currentFileId !== undefined}
  {#await $rootState.getClient() then client}
    <FileBrowser {client} {currentFileId} />
  {/await}
{/if}

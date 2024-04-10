<script lang="ts">
  import { page } from "$app/stores";
  import { onMount } from "svelte";

  import FileBrowser from "../../../components/FileBrowser.svelte";
  import { RootState } from "$lib/states/root-state";
  import { onNavigate } from "$app/navigation";

  const rootState = RootState.state;

  let currentFileId: number | null | undefined = undefined;

  function update(url: URL) {
    const currentId = url.pathname.split("/")[3] ?? null;

    currentFileId =
      (currentId != null ? Number.parseInt(currentId) : null) ?? null;
  }

  onMount(() => {
    update($page.url);
  });

  onNavigate((navigation) => {
    update(navigation.to?.url ?? navigation.from?.url ?? $page.url);
  });
</script>

{#if currentFileId !== undefined}
  {#await $rootState.getClient() then client}
    <FileBrowser {client} bind:currentFileId />
  {/await}
{/if}

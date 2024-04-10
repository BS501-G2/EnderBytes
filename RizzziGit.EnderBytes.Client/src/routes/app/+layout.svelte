<script lang="ts">
  import { onMount } from "svelte";

  import type { Client } from "$lib/client/client";
  import { RootState } from "$lib/states/root-state";

  import Dashboard from "../../components/Dashboard.svelte";
  import LoadingPage from "../../components/LoadingPage.svelte";

  const rootState = RootState.state;

  let loadPromise: Promise<Client> | null = null;
  async function load(): Promise<Client> {
    return await $rootState.getClient();
  }

  onMount(() => (loadPromise = load()));
</script>

{#if loadPromise == null}
  <div class="loading">
    <LoadingPage />
  </div>
{:else}
  {#await loadPromise}
    <div class="loading">
      <LoadingPage />
    </div>
  {:then client}
    <Dashboard {client}>
      <slot />
    </Dashboard>
  {/await}
{/if}

<style lang="scss">
  div.loading {
    width: 100vw;
    height: 100vh;
  }
</style>

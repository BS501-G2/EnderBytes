<script lang="ts">
  import { RootState } from "$lib/states/root-state";
  import type { Client } from "$lib/client/client";
  import { navigating } from "$app/stores";

  import Dashboard from "../../components/Dashboard.svelte";
  import Awaiter, {
    type AwaiterCallback,
  } from "../../components/Awaiter.svelte";
  import LoadingPage from "../../components/LoadingSpinnerPage.svelte";
  import LoadingBar from "../../components/LoadingBar.svelte";

  const rootState = RootState.state;

  const callback: AwaiterCallback<Client> = async (
    setStatus,
  ): Promise<Client> => {
    setStatus("Loading .NET Libraries...");

    return $rootState.getClient();
  };
</script>

<Awaiter {callback}>
  <svelte:fragment slot="success" let:result={client}>
    <Dashboard {client}>
      {#if $navigating}
        <div class="top-loading">
          <LoadingBar />
        </div>
      {/if}
      <slot />
    </Dashboard>
  </svelte:fragment>

  <svelte:fragment slot="loading" let:message>
    <div class="loading-page">
      <LoadingPage>
        <p slot="with-spinner">{message}</p>
      </LoadingPage>
    </div>
  </svelte:fragment>
</Awaiter>

<style lang="scss">
  div.loading-page {
    min-height: 100vh;

    display: flex;
    justify-content: center;
    align-items: center;
  }

  div.top-loading {
    height: 0px;
  }
</style>

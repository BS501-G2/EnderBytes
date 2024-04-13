<script lang="ts">
  import { navigating } from "$app/stores";

  import Dashboard from "../../components/Dashboard/Dashboard.svelte";
  import LoadingPage from "../../components/Widgets/LoadingSpinnerPage.svelte";
  import LoadingBar from "../../components/Widgets/LoadingBar.svelte";
  import { pendingTasks } from "../../components/BackgroundTaskList/BackgroundTaskList.svelte";
  import ClientAwaiter from "../../components/Bindings/ClientAwaiter.svelte";
</script>

<svelte:window
  on:beforeunload={(event) => {
    if ($pendingTasks.length == 0) {
      return;
    }

    event.preventDefault();
    return "There are pending tasks in the queue. Are you sure you want to leave?";
  }}
/>

<ClientAwaiter>
  <svelte:fragment let:client>
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
        <p>{message}</p>
      </LoadingPage>
    </div>
  </svelte:fragment>
</ClientAwaiter>

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

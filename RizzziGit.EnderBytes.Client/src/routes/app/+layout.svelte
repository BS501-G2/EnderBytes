<script lang="ts">
  import { goto } from '$app/navigation'
  import { navigating, page } from "$app/stores";

  import Dashboard from "../../components/Dashboard.svelte";
  import LoadingBar from "../../components/Widgets/LoadingBar.svelte";
  import { pendingTasks } from "../../components/BackgroundTaskList.svelte";
  import { session } from '../../components/Bindings/Client.svelte';

  $: {
    if ($session == null) {
      if (!$page.url.pathname.startsWith('/app/auth')) {
        goto('/app/auth/login', { replaceState: true });
      }
    } else {
      if ($page.url.pathname.startsWith('/app/auth')) {
        goto('/app', { replaceState: true });
      }
    }
  }
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

<Dashboard>
  {#if $navigating}
    <div class="top-loading">
      <LoadingBar />
    </div>
  {/if}
  <slot />
</Dashboard>

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

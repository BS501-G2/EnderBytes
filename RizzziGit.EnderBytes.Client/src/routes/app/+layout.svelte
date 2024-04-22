<script lang="ts">
  import { goto } from '$app/navigation'
  import { navigating, page } from "$app/stores";

  import Dashboard from "../../components/Dashboard.svelte";
  import LoadingBar from "../../components/Widgets/LoadingBar.svelte";
  import { runningBackgroundTasks } from "../../components/BackgroundTaskList.svelte";
  import { session } from '../../components/Bindings/Client.svelte';

  $: {
    if ($session == null) {
      if (!$page.url.pathname.startsWith('/app/auth')) {
        goto(`/app/auth/login?return=${decodeURIComponent($page.url.pathname)}`, { replaceState: true });
      }
    } else {
      if ($page.url.pathname.startsWith('/app/auth')) {
        goto($page.url.searchParams.get('return') ?? '/app', { replaceState: true });
      }
    }
  }
</script>

<svelte:window
  on:beforeunload={(event) => {
    if ($runningBackgroundTasks.length == 0) {
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
  div.top-loading {
    height: 0px;
  }
</style>

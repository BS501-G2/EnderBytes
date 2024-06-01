<script lang="ts">
  import { type Snippet } from 'svelte';
  import { navigating } from '$app/stores';

  import Dashboard from './app.svelte';
  import { runningBackgroundTasks } from '$lib/background-task.svelte';
  import { LoadingBar } from '@rizzzi/svelte-commons';

  const { children }: { children: Snippet } = $props();
</script>

<svelte:window
  on:beforeunload={(event) => {
    if ($runningBackgroundTasks.length == 0) {
      return;
    }

    event.preventDefault();
    return 'There are pending tasks in the queue. Are you sure you want to leave?';
  }}
/>

<Dashboard>
  {#if $navigating}
    <div class="top-loading">
      <LoadingBar />
    </div>
  {/if}
  {@render children()}
</Dashboard>

<style lang="scss">
  div.top-loading {
    height: 0px;
  }
</style>

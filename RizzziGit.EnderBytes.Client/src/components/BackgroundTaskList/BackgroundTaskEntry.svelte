<script lang="ts">
  import type { Writable } from "svelte/store";
  import {
    BackgroundTaskStatus,
    type BackgroundTask,
  } from "./BackgroundTaskList.svelte";
  import { PlayIcon, RefreshCwIcon, XIcon } from "svelte-feather-icons";
  import LoadingBar from "../Widgets/LoadingBar.svelte";

  export let backgroundTask: Writable<BackgroundTask<any>>;
  export let filter: BackgroundTaskStatus[];
  export let index: number;
</script>

{#if filter.includes($backgroundTask.status)}
  {#if index != 0}
    <div class="divider" />
  {/if}

  <div class="background-task">
    <div class="name">
      <p><b>{$backgroundTask.name}</b></p>

      {#if $backgroundTask.status == BackgroundTaskStatus.Running}
        {#if !$backgroundTask.cancelled}
          <button on:click={() => $backgroundTask.cancel()} title="Cancel"
            ><XIcon size="16px" /></button
          >
        {/if}
      {:else if $backgroundTask.status == BackgroundTaskStatus.Done}
        <button on:click={() => $backgroundTask.dismiss()} title="Dismiss"
          ><XIcon size="16px" /></button
        >
      {:else if $backgroundTask.status == BackgroundTaskStatus.Failed}
        {#if $backgroundTask.retryable}
          <button on:click={() => $backgroundTask.run()} title="Run"
            ><RefreshCwIcon size="16px" /></button
          >
        {/if}
        <button on:click={() => $backgroundTask.dismiss()} title="Dismiss"
          ><XIcon size="16px" /></button
        >
      {:else if $backgroundTask.status == BackgroundTaskStatus.Ready}
        <button on:click={() => $backgroundTask.run()} title="Run"
          ><PlayIcon size="16px" /></button
        >
        <button on:click={() => $backgroundTask.dismiss()} title="Dismiss"
          ><XIcon size="16px" /></button
        >
      {/if}
    </div>
    {#if $backgroundTask.status == BackgroundTaskStatus.Running}
      <div class="progress">
        <LoadingBar bind:progress={$backgroundTask.progress} />
      </div>
    {/if}
    <div class="message">
      {#if $backgroundTask.status == BackgroundTaskStatus.Failed}
        <p>Failed: {$backgroundTask.message}</p>
      {:else}
        {#if $backgroundTask.message != null}
          <p>{$backgroundTask.message}</p>
        {/if}
        {#if $backgroundTask.progress != null}
          <p>{Math.round($backgroundTask.progress * 1000) / 10}%</p>
        {/if}
      {/if}
    </div>
  </div>
{/if}

<style lang="scss">
  div.background-task {
    font-size: 10px;
    display: flex;
    flex-direction: column;

    gap: 8px;

    > div.name {
      display: flex;
      flex-direction: row;
      align-items: center;

      gap: 9px;

      > p {
        margin: 0px;
      }

      > p:nth-child(1) {
        flex-grow: 1;
        min-width: 0px;
      }

      > button {
        background-color: unset;
        border: unset;
        color: var(--primary);

        cursor: pointer;

        padding: 0px;

        width: 16px;
        height: 16px;
      }
    }

    > div.message {
      display: flex;
      flex-direction: row;
      align-items: center;

      > p {
        margin: 0px;
      }

      > p:nth-child(1) {
        flex-grow: 1;
        min-width: 0px;
      }

      > p:nth-child(2) {
        max-width: 100%;
      }
    }
  }
</style>

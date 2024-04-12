<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { RootState } from "$lib/states/root-state";
  import {
    executeBackgroundTask,
    type BackgroundTaskSetStatusFunction,
  } from "../BackgroundTaskList/BackgroundTaskList.svelte";
  import Awaiter, {
    type AwaiterCallback,
    type AwaiterSetStatusFunction,
  } from "./Awaiter.svelte";

  interface $$Slots {
    default: { client: Client };
    "not-loaded": {};
    loading: { message: string | null };
    "loading-without-spinner": { message: string | null };
    "loading-with-spinner": { message: string | null };
    error: { error: Error };
  }

  const rootState = RootState.state;

  const bindSetStatus: (
    setBackgroundTaskStatus: BackgroundTaskSetStatusFunction,
    setAwaiterStatus: AwaiterSetStatusFunction,
  ) => (message?: string | null, progress?: number | null) => void =
    (setBackgroundTaskStatus, setAwaiterStatus) =>
    (message = null, progress = null) => {
      setBackgroundTaskStatus(message, progress);
      setAwaiterStatus(message, progress);
    };

  const load: AwaiterCallback<Client> = async (
    setAwaiterStatus,
  ): Promise<Client> => {
    const backgroundTaskClient = executeBackgroundTask(
      "Client Initialization",
      true,
      (_, setBackgroundTaskStatus) => {
        const setStatus = bindSetStatus(
          setBackgroundTaskStatus,
          setAwaiterStatus,
        );
        setStatus("Importing .NET client dependencies...");
        return $rootState.getClient();
      },
      true,
    );

    return <Client>await backgroundTaskClient.run();
  };
</script>

<Awaiter callback={load}>
  <svelte:fragment slot="success" let:result={client}>
    {#if $$slots.default}
      <slot {client} />
    {/if}
  </svelte:fragment>

  <svelte:fragment slot="not-loaded">
    {#if $$slots["not-loaded"]}
      <slot name="not-loaded" />
    {/if}
  </svelte:fragment>

  <svelte:fragment slot="loading" let:message>
    {#if $$slots.loading}
      <slot name="loading" {message} />
    {/if}
  </svelte:fragment>

  <svelte:fragment slot="loading-without-spinner" let:message>
    {#if $$slots["loading-without-spinner"]}
      <slot name="loading-without-spinner" {message} />
    {/if}
  </svelte:fragment>

  <svelte:fragment slot="loading-with-spinner" let:message>
    {#if $$slots["loading-with-spinner"]}
      <slot name="loading-with-spinner" {message} />
    {/if}
  </svelte:fragment>

  <svelte:fragment slot="error" let:error>
    {#if $$slots.error}
      <slot name="error" {error} />
    {/if}
  </svelte:fragment>
</Awaiter>

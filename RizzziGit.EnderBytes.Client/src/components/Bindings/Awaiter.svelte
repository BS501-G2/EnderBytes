<script lang="ts" context="module">
  export type AwaiterSetStatusFunction = (
    message?: string | null,
    progressPercentage?: number | null,
  ) => void;
  export type AwaiterCallback<T> = (
    setStatus: AwaiterSetStatusFunction,
  ) => T | Promise<T>;

  export interface AwaiterTask {}

  export class AwaiterState {
    public constructor() {}
  }
</script>

<script lang="ts" generics="T extends any">
  import { onMount } from "svelte";
  import LoadingPage from "../Widgets/LoadingSpinnerPage.svelte";
  import Banner, { BannerClass } from "../Widgets/Banner.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";

  export let callback: AwaiterCallback<T>;
  export let reload: (() => void) | null = null;
  export let autoLoad = true;

  let promise: Promise<T>;
  let message: string | null = null;
  let progress: number | null = null;

  const setStatus: AwaiterSetStatusFunction = (
    newMessage = message,
    newProgress = progress,
  ) => {
    message = newMessage;
    progress = newProgress;
  };

  async function load() {
    return await callback(setStatus);
  }

  async function exec() {
    try {
      promise = load();
    } catch (error: any) {
      promise = Promise.reject(error);
    }
  }

  onMount(() => {
    if (autoLoad) {
      exec();
    }

    reload = exec;
  });
</script>

{#if promise == null}
  {#if $$slots["not-loaded"]}
    <slot name="not-loaded" />
  {:else if $$slots.loading}
    <slot name="loading" {message} />
  {:else}
    <Banner bannerClass={BannerClass.Info}>
      <div class="banner">
        <p style="margin: 0">Not loaded.</p>
        <Button onClick={exec} buttonClass={ButtonClass.Background}>Load</Button
        >
      </div>
    </Banner>
  {/if}
{:else}
  {#await promise}
    {#if $$slots.loading}
      <slot name="loading" {message} />
    {:else if $$slots["loading-without-spinner"]}
      <LoadingPage>
        <svelte:fragment slot="without-spinner">
          <slot name="loading-without-spinner" {message} />
        </svelte:fragment>
      </LoadingPage>
    {:else if $$slots["loading-with-spinner"]}
      <LoadingPage>
        <svelte:fragment slot="with-spinner">
          <slot name="loading-with-spinner" {message} />
        </svelte:fragment>
      </LoadingPage>
    {:else}
      <LoadingPage>
        <svelte:fragment slot="with-spinner">
          {#if message == null || progress == null}
            <p>
              {#if message != null}
                {message}
              {/if}

              {#if progress != null}
                {Math.floor(progress * 100)}%
              {/if}
            </p>
          {/if}
        </svelte:fragment>
      </LoadingPage>
    {/if}
  {:then result}
    <slot name="success" {result} />
  {:catch error}
    {#if $$slots.error}
      <slot name="error" {error} retry={exec} />
    {:else}
      <div class="container">
        <Banner bannerClass={BannerClass.Error}>
          <div class="banner">
            <p style="margin: 0">Error: {error.message}</p>
            <Button onClick={exec} buttonClass={ButtonClass.Background}
              >Retry</Button
            >
          </div>
        </Banner>
      </div>
    {/if}
  {/await}
{/if}

<style lang="scss">
  div.container {
    display: flex;
    flex-direction: column;
    gap: 8px;
    justify-content: center;

    width: 100%;
    height: 100%;

    div.banner {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
  }
</style>

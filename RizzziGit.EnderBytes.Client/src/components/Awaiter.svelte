<script lang="ts" context="module">
  export type AwaiterCallback<T> = (
    setMessage: (message: string | null) => void,
  ) => T | Promise<T>;
</script>

<script lang="ts" generics="T extends any">
  import { onMount } from "svelte";
  import LoadingPage from "./LoadingPage.svelte";
  import Banner, { BannerClass } from "./Banner.svelte";
  import Button, { ButtonClass } from "./Button.svelte";

  export let callback: AwaiterCallback<T>;
  export let reload: (() => void) | null = null;
  export let autoLoad = true;

  let promise: Promise<T>;
  let message: string | null = null;

  async function setMessage(newMessage: string | null) {
    message = newMessage;
  }

  async function load() {
    return await callback(setMessage);
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
    <slot name="loading" />
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
        <p slot="with-spinner">{message}</p>
      </LoadingPage>
    {/if}
  {:then result}
    <slot name="success" {result} />
  {:catch error}
    {#if $$slots.error}
      <slot name="error" {error} retry={exec} />
    {:else}
      <Banner bannerClass={BannerClass.Error}>
        <div class="banner">
          <p style="margin: 0">Error: {error.message}</p>
          <Button onClick={exec} buttonClass={ButtonClass.Background}
            >Retry</Button
          >
        </div>
      </Banner>
    {/if}
  {/await}
{/if}

<style lang="scss">
  div.banner {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
</style>

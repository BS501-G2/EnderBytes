<script lang="ts">
  import Loading from "./Loading.svelte";
</script>

<div class="loading-page">
  {#if $$slots["without-spinner"]}
    <slot name="without-spinner" />
  {:else}
    <div class="loading-icon">
      <Loading />
    </div>
    {#if $$slots["with-spinner"]}
      <slot name="with-spinner" />
    {/if}
  {/if}
</div>

<style lang="scss">
  div.loading-page {
    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;

    > div.loading-icon {
      max-width: 128px;
      max-height: 128px;
    }
  }
</style>

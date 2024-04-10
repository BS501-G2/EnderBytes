<script>
  import Loading from "./Loading.svelte";
</script>

<div class="loading-page">
  <div class="loading-icon">
    <Loading />
  </div>
</div>

<style lang="scss">
  div.loading-page {
    width: 100%;
    height: 100%;

    display: flex;
    justify-content: center;
    align-items: center;

    > div.loading-icon {
      max-width: 128px;
      max-height: 128px;
    }
  }
</style>

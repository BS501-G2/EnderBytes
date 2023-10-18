<script lang="ts">
</script>

<style lang="postcss">
  div.main-panel-panel {
    width: calc(100% - var(--panel-width-contracted));
    height: 100%;

    position: absolute;

    left: var(--panel-width-contracted);
    top: 0px;

    background-color: var(--main-panel-background);
  }
</style>

<div class="main-panel-panel">
  <slot />
</div>
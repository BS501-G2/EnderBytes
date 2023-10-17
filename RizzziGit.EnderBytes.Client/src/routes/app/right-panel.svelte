<script lang="ts">

</script>

<style lang="postcss">
  div.right-panel {
    width: calc(100% - var(--panel-width));
    height: 100%;

    position: absolute;

    left: var(--panel-width);
    top: 0px;
  }
</style>
<div class="right-panel">
  <slot />
</div>
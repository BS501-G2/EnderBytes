<script lang="ts">
  export let rotate: number
  export let distance: number
  export let x: number
  export let y: number
</script>

<style lang="postcss">
  div.loading-bounds {
    position: fixed;
  }

  div.loading-particle {
    width: 16px;
    height: 16px;

    position: fixed;

    background-color: aqua;
  }
</style>

<div class="loading-bounds" style="transform=rotate({rotate}deg); left={x}px; top={y}px;">
  <div class="loading-particle" style="left={distance}px;">
  </div>
</div>
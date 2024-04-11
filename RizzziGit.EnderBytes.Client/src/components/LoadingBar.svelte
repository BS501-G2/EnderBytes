<script lang="ts">
  import AnimationFrame from "./AnimationFrame.svelte";

  let payload = { offset: 0 };

  let barWidth: number = 0;
  let thumbWidth: number = 0;
  $: maxWidth = barWidth + thumbWidth * 1;
</script>

<AnimationFrame
  callback={() => {
    payload.offset += barWidth / 100;

    if (payload.offset >= maxWidth) {
      payload.offset = 0;
    }
  }}
>
  <div class="loading-bar" bind:clientWidth={barWidth}>
    <div
      class="loading-thumb"
      bind:clientWidth={thumbWidth}
      style="margin-left: {payload.offset - thumbWidth}px;"
    ></div>
  </div>
</AnimationFrame>

<style lang="scss">
  div.loading-bar {
    width: 100%;
    height: 4px;

    background-color: var(--primaryContainer);

    display: flex;

    overflow-x: hidden;

    > div.loading-thumb {
      min-width: 25%;

      background-color: var(--primary);
    }
  }
</style>

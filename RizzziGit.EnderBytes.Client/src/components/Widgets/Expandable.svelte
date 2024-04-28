<script lang="ts">
  import { tweened } from "svelte/motion";
  import { cubicOut } from "svelte/easing";

  export let expanded: boolean = false;

  const height = tweened(0, {
    duration: 500,
    easing: cubicOut,
  });

  let contentHeight: number;

  $: $height = expanded ? contentHeight : 0;
</script>

<div class="expandable-header">
  <slot
    name="header"
    {expanded}
    toggle={() => {
      expanded = !expanded;
    }}
  />
</div>
<div
  class="expandable-body"
  style={$height == contentHeight ? "" : `max-height: ${$height}px;`}
>
  <div class="content" bind:clientHeight={contentHeight}>
    <slot name="body" />
  </div>
</div>

<style lang="scss">
  div.expandable-body {
    overflow-y: hidden;
  }
</style>

<script lang="ts">
  import {
    AlertTriangleIcon,
    CheckSquareIcon,
    RefreshCwIcon,
  } from "svelte-feather-icons";
  import {
    failedTasks,
    runningBackgroundTasks,
  } from "../../../../BackgroundTaskList.svelte";
  import type { FrameCallback } from "../../../../Bindings/AnimationFrame.svelte";
  import AnimationFrame from "../../../../Bindings/AnimationFrame.svelte";
  import TitleBarChipButton from "../TitleBarChip/Button.svelte";

  const operationsIcon: FrameCallback<number> = (p, t, value = 0) => {
    value += 2;

    while (value >= 360) {
      value -= 360;
    }

    return value;
  };

  export let iconSize: string;
</script>

<TitleBarChipButton onClick={() => {}}>
  {#if $runningBackgroundTasks.length > 0}
    <AnimationFrame callback={operationsIcon} let:output>
      <div class="spinner" style="transform: rotate({output}deg);">
        <RefreshCwIcon size={iconSize} />
      </div>
    </AnimationFrame>
  {:else if $failedTasks.length > 0}
    <AlertTriangleIcon size={iconSize} />
  {:else}
    <CheckSquareIcon size={iconSize} />
  {/if}
</TitleBarChipButton>

<style lang="scss">
  div.spinner {
    display: flex;
    align-items: center;
    justify-content: center;
  }
</style>

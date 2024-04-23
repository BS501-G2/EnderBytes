<script lang="ts" context="module">
  import { writable, type Writable } from "svelte/store";

  export let enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import {
    AlertTriangleIcon,
    CheckSquareIcon,
    RefreshCwIcon,
  } from "svelte-feather-icons";
  import BackgroundTaskList, {
    failedBackgroundTasks,
    runningBackgroundTasks,
    BackgroundTaskStatus,
    completedBackgroundTasks,
    dismissAll,
  } from "../../../../BackgroundTaskList.svelte";
  import type { FrameCallback } from "../../../../Bindings/AnimationFrame.svelte";
  import AnimationFrame from "../../../../Bindings/AnimationFrame.svelte";
  import TitleBarChipButton from "../TitleBarChip/Button.svelte";
  import Overlay, {
    OverlayPositionType,
  } from "../../../../Widgets/Overlay.svelte";
  import { onDestroy, onMount } from "svelte";
  import LoadingBar from "../../../../Widgets/LoadingBar.svelte";
  import Button, { ButtonClass } from "../../../../Widgets/Button.svelte";

  const operationsIcon: FrameCallback<number> = (p, t, value = 0) => {
    value += 2;

    while (value >= 360) {
      value -= 360;
    }

    return value;
  };

  export let iconSize: string;

  export let updateMenuLocation: () => void;
  export let menuX: number;
  export let menuY: number;

  let update: number = 0;

  let unsubscribe: () => void;

  onMount(() => {
    unsubscribe = runningBackgroundTasks.subscribe(() => {
      update = 10000;
      updateMenuLocation();
    });
  });

  onDestroy(() => {
    unsubscribe();
  });

  let cooldownTime: number = 10000;
</script>

{#if update > 0}
  <AnimationFrame
    callback={(previous, current) => {
      if ($runningBackgroundTasks.length > 0) {
        update = cooldownTime;
      } else if ($failedBackgroundTasks.length > 0) {
        update -= current - (previous ?? Date.now());
      } else {
        update = 0;
      }
    }}
  />
{/if}

{#if $enabled || update > 0}
  <Overlay
    position={$enabled
      ? [OverlayPositionType.Offset, -menuX, menuY]
      : [OverlayPositionType.Offset, -16, -16]}
    onDismiss={$enabled ? () => ($enabled = false) : null}
  >
    <div class="operations-menu">
      {#if $enabled}
        <div class="menu-header">
          <h2>Operations</h2>
          {#if $failedBackgroundTasks.length > 0 || $runningBackgroundTasks.length > 0}
            <Button
              onClick={dismissAll}
              buttonClass={ButtonClass.PrimaryContainer}>Clear</Button
            >
          {/if}
        </div>

        <div class="divider" />
        <BackgroundTaskList />
      {:else if update != 0}
        {#if update != cooldownTime}
          <LoadingBar progress={(cooldownTime - update) / cooldownTime} />
        {/if}
        <BackgroundTaskList
          filter={[BackgroundTaskStatus.Running, BackgroundTaskStatus.Failed]}
        />
      {/if}
    </div>
  </Overlay>
{/if}

<TitleBarChipButton
  onClick={() => {
    $enabled = true;
    updateMenuLocation();
  }}
>
  {#if $runningBackgroundTasks.length > 0}
    <AnimationFrame callback={operationsIcon} let:output>
      <div class="spinner" style="transform: rotate({output}deg);">
        <RefreshCwIcon size={iconSize} />
      </div>
    </AnimationFrame>
  {:else if $failedBackgroundTasks.length > 0}
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

  div.operations-menu {
    background-color: var(--primary);
    color: var(--onPrimary);

    min-width: 512px;
    max-width: 512px;

    display: flex;
    flex-direction: column;

    box-sizing: border-box;

    gap: 4px;
    padding: 8px 16px 8px 16px;

    box-shadow: 2px 2px 8px #0000007f;

    border-radius: 8px;

    > div.menu-header {
      display: flex;
      flex-direction: row;
      align-items: center;

      > h2 {
        text-align: left;

        flex-grow: 1;
      }
    }

    > div.divider {
      width: 100%;
      height: 1px;
      background-color: var(--onPrimary);
    }
  }
</style>

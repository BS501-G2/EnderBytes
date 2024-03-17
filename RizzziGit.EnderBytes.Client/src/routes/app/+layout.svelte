<script lang="ts">
  import {
    DisplayMode,
    removeOnDisplayModeChangeListener,
    addOnDisplayModeChangeListener,
    type OnDisplayModeChangeListener,
    getDisplayMode,
  } from "$lib/display-mode";

  import { onMount, onDestroy } from "svelte";
  import SideBar from "./side-bar.svelte";

  let displayMode: DisplayMode = getDisplayMode();

  const onDisplayModeChangeListener: OnDisplayModeChangeListener = (
    newDisplayMode,
  ) => (displayMode = newDisplayMode);

  onDestroy(() => {
    removeOnDisplayModeChangeListener(onDisplayModeChangeListener);
  });

  onMount(() => {
    addOnDisplayModeChangeListener(onDisplayModeChangeListener);
  });
</script>

<SideBar />
<div class="app-main-section">
  <p class="indicator">PWA: {DisplayMode[displayMode] ?? "Unknown"}</p>

  <slot />
</div>

<style lang="scss">
  p.indicator {
    position: fixed;

    left: 0px;
    top: 0px;
  }

  div.app-main-section {
    margin-left: 96px;

    padding: 16px;

    @media only screen and (max-width: 720px) {
      margin-left: unset;

      margin-bottom: 96px;
    }
  }
</style>
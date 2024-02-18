<script lang="ts">
  import {
    DisplayMode,
    removeOnDisplayModeChangeListener,
    addOnDisplayModeChangeListener,
    type OnDisplayModeChangeListener,
    getDisplayMode,
  } from "$lib/display-mode";
  import { onMount, onDestroy } from "svelte";

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

<p>PWA: {DisplayMode[displayMode] ?? "Unknown"}</p>
<slot />

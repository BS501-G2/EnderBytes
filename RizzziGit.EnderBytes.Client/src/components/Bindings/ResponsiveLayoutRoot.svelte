<script lang="ts" context="module">
  import { onMount } from "svelte";
  import { writable, type Writable } from "svelte/store";

  export enum ViewMode {
    Unset = 0,

    Mobile = 0b0000001,
    Desktop = 0b0000010,
    Browser = 0b0000100,
    Standalone = 0b0001000,
    Fullscreen = 0b0010000,
    MinimalUI = 0b0100000,
    OverlayControls = 0b1000000,
  }

  export const viewMode: Writable<ViewMode> = writable(ViewMode.Unset);
</script>

<script lang="ts">
  function onResize() {
    let newViewMode: ViewMode = ViewMode.Unset;

    newViewMode |= window.matchMedia("(max-width: 768px)").matches
      ? ViewMode.Mobile
      : ViewMode.Desktop;

    newViewMode |= window.matchMedia("(display-mode: minimal-ui)").matches
      ? ViewMode.MinimalUI
      : window.matchMedia("(display-mode: window-controls-overlay)").matches
        ? ViewMode.OverlayControls
        : window.matchMedia("(display-mode: standalone)").matches
          ? ViewMode.Standalone
          : ViewMode.Browser;

    if (newViewMode != $viewMode) {
      $viewMode = newViewMode;

      console.log(ViewMode[$viewMode]);
    }
  }

  onMount(onResize);
</script>

<svelte:window on:resize={onResize} />

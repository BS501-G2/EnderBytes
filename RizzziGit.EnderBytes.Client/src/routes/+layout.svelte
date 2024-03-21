<script lang="ts" context="module">
  import { setContext, onMount } from "svelte";
  import { writable } from "svelte/store";

  import { ViewMode } from "$lib/view-mode";
  import { STATE_ROOT, APP_NAME, APP_TAGLINE } from "$lib/values";
  import { Theme, serializeThemeColorsIntoInlineStyle } from "$lib/themes";

  export class RootState {
    public constructor() {
      this.theme = Theme.Ender;

      this.viewMode = ViewMode.Unset;
    }

    theme: Theme;

    viewMode: ViewMode;
  }

  const rootState = writable<RootState>(new RootState());
</script>

<script lang="ts">
  setContext(STATE_ROOT, rootState);

  function onResize() {
    $rootState.viewMode =
      (window.matchMedia("(max-width: 720px)").matches
        ? ViewMode.Mobile
        : ViewMode.Desktop) |
      (window.matchMedia("(display-mode: standalone)").matches
        ? ViewMode.Standalone
        : window.matchMedia("(display-mode: fullscreen)").matches
          ? ViewMode.Fullscreen
          : ViewMode.Browser);
  }

  onMount(() => {
    rootState.subscribe((value) =>
      document.documentElement.setAttribute(
        "style",
        serializeThemeColorsIntoInlineStyle(value.theme),
      ),
    );

    $rootState.theme =
      <Theme | null>localStorage.getItem("theme") ?? Theme.Ender;

    onResize();
  });
</script>

<svelte:head>
  <title>{APP_NAME} - {APP_TAGLINE}</title>

  <script type="module" src="/dotnet.js" />
</svelte:head>

<svelte:window on:resize={onResize} />

{#if $rootState.viewMode != ViewMode.Unset}
  <slot />
{/if}

<style lang="scss">
  :root {
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;

    background-color: var(--background);

    color: var(--onBackground);

    min-width: 320px;
  }
</style>

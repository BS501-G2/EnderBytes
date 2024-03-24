<script lang="ts" context="module">
  import { setContext, onMount } from "svelte";
  import { writable } from "svelte/store";

  import { ViewMode } from "$lib/view-mode";
  import { STATE_ROOT } from "$lib/values";
  import {
    ColorScheme,
    serializeThemeColorsIntoInlineStyle,
  } from "$lib/color-schemes";

  import {
    Locale,
    bindLocalizedString,

    LOCALE_APP_NAME,
    LOCALE_APP_TAGLINE,
  } from "$lib/locale";

  export class RootState {
    public constructor() {
      this.theme = ColorScheme.Ender;

      this.viewMode = ViewMode.Unset;

      this.locale = Locale.en_US;
    }

    theme: ColorScheme;

    viewMode: ViewMode;

    locale: Locale;
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
    rootState.subscribe((value) => {
      document.documentElement.setAttribute(
        "style",
        serializeThemeColorsIntoInlineStyle(value.theme),
      );

      document.documentElement.setAttribute("lang", value.locale);
    });

    $rootState.theme =
      <ColorScheme | null>localStorage.getItem("theme") ?? ColorScheme.Ender;

    onResize();
  });

  const localizedString = bindLocalizedString(() => $rootState.locale);
</script>

<svelte:head>
  <title
    >{localizedString(LOCALE_APP_NAME)} - {localizedString(
      LOCALE_APP_TAGLINE,
    )}</title
  >
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

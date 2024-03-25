<script lang="ts" context="module">
  import { setContext, onMount, onDestroy } from "svelte";
  import { writable } from "svelte/store";

  import { ViewMode } from "$lib/view-mode";
  import { STATE_ROOT } from "$lib/values";
  import {
    ColorScheme,
    serializeThemeColorsIntoInlineStyle,
  } from "$lib/color-schemes";

  import { Locale, getString, LocaleKey } from "$lib/locale";

  export class RootState {
    public constructor() {
      this.theme = ColorScheme.Ender;
      this.viewMode = ViewMode.Unset;
      this.locale = Locale.en_US;
    }

    theme: ColorScheme;
    viewMode: ViewMode;
    locale: Locale;

    public getString<T extends LocaleKey>(
      key: T,
      params?: Record<string, string>,
    ) {
      return getString(this.locale, key, params);
    }
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

  let unsubscriber: () => void = () => {};

  onMount(() => {
    $rootState.theme =
      <ColorScheme | null>localStorage.getItem("theme") ?? ColorScheme.Ender;
    $rootState.locale =
      <Locale | null>localStorage.getItem("locale") ?? Locale.en_US;

    unsubscriber = rootState.subscribe((value) => {
      document.documentElement.setAttribute("lang", value.locale);
      document.documentElement.setAttribute(
        "style",
        serializeThemeColorsIntoInlineStyle(value.theme),
      );

      localStorage.setItem("locale", value.locale);
      localStorage.setItem("theme", value.theme);
    });

    onResize();
  });

  onDestroy(unsubscriber);
</script>

<svelte:head>
  <title
    >{$rootState.getString(LocaleKey.AppName)} - {$rootState.getString(
      LocaleKey.AppTagline,
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

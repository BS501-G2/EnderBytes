<script lang="ts">
  import { onMount } from "svelte";

  import { Locale, LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";
  import {
    ColorScheme,
    serializeThemeColorsIntoInlineStyle,
  } from "$lib/color-schemes";
  import { navigating } from "$app/stores";
  import LoadingSpinnerPage from "../components/Widgets/LoadingSpinnerPage.svelte";

  const rootState = RootState.state;

  function onResize() {
    const newViewMode =
      (window.matchMedia("(max-width: 768px)").matches
        ? ViewMode.Mobile
        : ViewMode.Desktop) |
      (window.matchMedia(
        "(display-mode: standalone) or (display-mode: window-controls-overlay) or (display-modee: minimal-ui)",
      ).matches
        ? ViewMode.Standalone
        : window.matchMedia("(display-mode: fullscreen)").matches
          ? ViewMode.Fullscreen
          : ViewMode.Browser);

    if (newViewMode != $rootState.viewMode) {
      $rootState.viewMode = newViewMode;
    }
  }

  function loadSettings() {
    $rootState.theme =
      <ColorScheme | null>localStorage.getItem("theme") ?? ColorScheme.Ender;
    $rootState.locale =
      <Locale | null>localStorage.getItem("locale") ?? Locale.en_US;
  }

  function saveSettings() {
    localStorage.setItem("locale", $rootState.locale);
    localStorage.setItem("theme", $rootState.theme);
  }

  onMount(() => {
    loadSettings();

    rootState.subscribe((value) => {
      document.documentElement.setAttribute("lang", value.locale);

      saveSettings();
    });

    onResize();
  });
</script>

<svelte:head>
  <title>
    {$rootState.getString(LocaleKey.AppName)} - {$rootState.getString(
      LocaleKey.AppTagline,
    )}
  </title>

  {@html `<style>:root { ${serializeThemeColorsIntoInlineStyle($rootState.theme)} } body { margin: unset; min-height: 100vh; }</style>`}
</svelte:head>

<svelte:window on:resize={onResize} />

{#if $rootState.viewMode != ViewMode.Unset || !$navigating}
  <slot />
{:else}
  <LoadingSpinnerPage></LoadingSpinnerPage>
{/if}

<style lang="scss">
  :root {
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;

    background-color: var(--background);

    color: var(--onBackground);

    min-width: 320px;
  }
</style>

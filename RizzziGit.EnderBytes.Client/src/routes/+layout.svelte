<script lang="ts">
  import "@fortawesome/fontawesome-free/css/all.min.css";
  import "./reset.scss";

  import { onMount } from "svelte";

  import { Locale, LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";
  import {
    ColorScheme,
    serializeThemeColorsIntoInlineStyle,
  } from "$lib/color-schemes";
  import ResponsiveLayoutRoot, {
    viewMode,
    ViewMode,
  } from "../components/Bindings/ResponsiveLayoutRoot.svelte";

  const rootState = RootState.state;

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
  });

  $: rootCss = `:root { ${serializeThemeColorsIntoInlineStyle($rootState.theme)} } body { margin: unset; min-height: 100vh; background-color: var(--background); }`;
</script>

<svelte:head>
  <title>
    {$rootState.getString(LocaleKey.AppName)} - {$rootState.getString(
      LocaleKey.AppTagline,
    )}
  </title>

  {@html `<style>
    :root {
      ${serializeThemeColorsIntoInlineStyle($rootState.theme)}
    }

    body {
      margin: unset;
      min-height: 100vh;
      background-color: var(--background);
    }
  </style>`}
</svelte:head>

<slot />

<ResponsiveLayoutRoot />

<style lang="scss">
  :root {
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;

    background-color: var(--background);

    color: var(--onBackground);

    min-width: 320px;
  }
</style>

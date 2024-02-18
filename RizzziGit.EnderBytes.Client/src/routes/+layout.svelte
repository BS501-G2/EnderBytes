<script lang="ts">
  import { page } from "$app/stores";
  import { onMount, onDestroy } from "svelte";

  import {
    Theme,
    type OnThemeChangeListener,
    getTheme,
    removeOnThemeChangeListener,
    addOnThemeChangeListener,
  } from "$lib/themes";

  import NavigationBar from "./navigation-bar.svelte";
  import { NavigationMenuItems } from "$lib/navigation-menu-items";
  import { APP_NAME, APP_TAGLINE } from "$lib/manifest";
  import {
    DisplayMode,
    type OnDisplayModeChangeListener,
    triggerUpdateCheck,
    removeOnDisplayModeChangeListener,
    addOnDisplayModeChangeListener
  } from "$lib/display-mode";

  let theme: Theme = Theme.Default;
  let displayMode: DisplayMode = DisplayMode.DesktopBrowser;
  let loaded: boolean = false

  const onThemeChangeListener: OnThemeChangeListener = (newTheme: Theme) => theme = newTheme;
  const onDisplayModeChangeListener: OnDisplayModeChangeListener = (newDisplayMode: DisplayMode) => displayMode = newDisplayMode;

  onDestroy(() => {
    removeOnThemeChangeListener(onThemeChangeListener);
    removeOnDisplayModeChangeListener(onDisplayModeChangeListener);
  });

  onMount(() => {
    theme = getTheme(window);

    addOnThemeChangeListener(onThemeChangeListener);
    addOnDisplayModeChangeListener(onDisplayModeChangeListener);

    triggerUpdateCheck(window);

    loaded = true
  });
</script>

<svelte:head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link rel="manifest" href="/site.webmanifest" />
  <link rel="shortcut icon" href="/favicon.png" />
  <link rel="stylesheet" href="/themes/{theme}.css" />

  {#if !$page.url.pathname.startsWith("/app")}
    <title>{APP_NAME} — {APP_TAGLINE}</title>
  {/if}

  <!-- <script src="https://cdn.tailwindcss.com"></script> -->
</svelte:head>

<svelte:window on:resize={() => triggerUpdateCheck(window)} />

{#if loaded}
  <slot />

  {#if !$page.url.pathname.startsWith("/app")}
    <NavigationBar {displayMode} menuItems={NavigationMenuItems} />
  {/if}

  <style lang="scss">
    :root {
      font-family: Arial, Helvetica, sans-serif;

      min-height: 1080px;
      min-width: 360px;

      background-color: rgb(250, 250, 250);
    }
  </style>
{/if}

<script lang="ts">
  import { page } from "$app/stores";
  import { onMount } from "svelte";

  import { ThemeManager, ThemeType } from "$lib/themes";

  import NavigationBar from "./navigation-bar.svelte";
  import { NavigationMenuItems } from "$lib/navigation-menu-items";

  let theme: ThemeType = ThemeType.Default;

  onMount(() => {
    theme = ThemeManager.get(localStorage);
  });
</script>

<svelte:head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link rel="icon" href="/favicon.png" />
  <link rel="stylesheet" href="/themes/{theme}.css" />
</svelte:head>

{#if $page.url.pathname != "/app"}
  <NavigationBar menuItems={NavigationMenuItems} />

{/if}

<slot />

<style lang="scss">
  :root {
    font-family: Arial, Helvetica, sans-serif;

    min-height: 1080px;
    min-width: 360px;

    background-color: rgb(250, 250, 250);
  }
</style>

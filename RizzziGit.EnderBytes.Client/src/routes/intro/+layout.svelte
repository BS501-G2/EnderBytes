<script lang="ts">
  import { ViewMode } from "$lib/view-mode";
  import { RootState } from "$lib/states/root-state";

  import NavigationBarDesktop from "./IntroNavigationBarDesktop.svelte";
  import NavigationBarMobile from "./IntroNavigationBarMobile.svelte";

  const rootState = RootState.state;

</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <NavigationBarDesktop />
{:else if $rootState.viewMode & ViewMode.Mobile}
  <NavigationBarMobile />
{/if}

<div style="display: contents; margin-top: {0}px; margin-bottom: {0}px;"></div>
<slot />

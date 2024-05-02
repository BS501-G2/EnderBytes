<script lang="ts">
  import { ViewMode } from "$lib/view-mode";
  import { RootState } from "$lib/states/root-state";

  import NavigationBarDesktop from "./IntroNavigationBarDesktop.svelte";
  import NavigationBarMobile from "./IntroNavigationBarMobile.svelte";
  import ResponsiveLayout from "../../components/Bindings/ResponsiveLayout.svelte";

  const rootState = RootState.state;
</script>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <NavigationBarDesktop />
  </svelte:fragment>
  <svelte:fragment slot="mobile">
    <NavigationBarMobile />
  </svelte:fragment>
</ResponsiveLayout>

<div style="display: contents; margin-top: {0}px; margin-bottom: {0}px;"></div>
<slot />

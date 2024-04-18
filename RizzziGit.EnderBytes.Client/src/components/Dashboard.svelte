<script lang="ts">
  import { page } from "$app/stores";

  import { RootState } from "$lib/states/root-state";
  import { LocaleKey } from "$lib/locale";

  import DesktopLayout from "./Dashboard/DesktopLayout.svelte";
  import MobileLayout from "./Dashboard/MobileLayout/MobileLayout.svelte";
  import ResponsiveLayout from "./Bindings/ResponsiveLayout.svelte";
  import AccountSettingsDialog from "./AccountSettingsDialog.svelte";
  import LogoutConfirmationDialog from "./Dashboard/LogoutConfirmationDialog.svelte";

  const rootState = RootState.state;
</script>

<svelte:head>
  <link
    rel="manifest"
    href="/manifest.json?locale={$rootState.locale}&theme={$rootState.theme}"
  />
  <title>{$rootState.getString(LocaleKey.AppName)}</title>
</svelte:head>

{#if $page.url.pathname.startsWith("/app/auth")}
  <slot />
{:else}
  <ResponsiveLayout>
    <svelte:fragment slot="desktop">
      <DesktopLayout>
        <slot />
      </DesktopLayout>
    </svelte:fragment>
    <svelte:fragment slot="mobile">
      <MobileLayout>
        <slot />
      </MobileLayout>
    </svelte:fragment>
  </ResponsiveLayout>

  <LogoutConfirmationDialog />
  <AccountSettingsDialog />
{/if}

<script lang="ts">
  import { page } from "$app/stores";

  import { RootState } from "$lib/states/root-state";
  import { LocaleKey } from "$lib/locale";

  import DesktopLayout from "./Dashboard/DesktopLayout.svelte";
  import MobileLayout from "./Dashboard/MobileLayout/MobileLayout.svelte";
  import ResponsiveLayout from "./Bindings/ResponsiveLayout.svelte";
  import AccountSettingsDialog from "./AccountSettingsDialog.svelte";
  import LogoutConfirmationDialog from "./Dashboard/LogoutConfirmationDialog.svelte";
  import Awaiter from "./Bindings/Awaiter.svelte";
  import Client from "./Bindings/Client.svelte";
    import LoadingSpinnerPage from "./Widgets/LoadingSpinnerPage.svelte";

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
  <Client let:fetch>
    <Awaiter callback={() => fetch("/user/!me")}>
      <svelte:fragment slot="success" let:result={user}>
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
      </svelte:fragment>
      <svelte:fragment slot="loading">
        <div class="loading" style="display: flex; width: 100vw; height: 100vh;">
          <LoadingSpinnerPage />
        </div>
      </svelte:fragment>
      <svelte:fragment slot="error">
        <div />
      </svelte:fragment>
    </Awaiter>
  </Client>
{/if}

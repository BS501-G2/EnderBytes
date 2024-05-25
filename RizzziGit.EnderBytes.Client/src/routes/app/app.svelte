<script lang="ts">
  import { page } from '$app/stores';

  import { RootState } from '$lib/states/root-state';
  import { getLocale } from '$lib/locale.svelte';

  import DesktopLayout from './-app/desktop.svelte';
  import MobileLayout from './-app/mobile.svelte';
  import AccountSettingsDialog from './-app/account-settings-dialog.svelte';
  import LogoutConfirmationDialog from './-app/logout-confirmation-dialog.svelte';
  import AppSettingsDialog from './-app/app-settings-dialog.svelte';
  import { ResponsiveLayout, currentColorScheme } from '@rizzzi/svelte-commons';
  import SettingsDialog from './settings-dialog.svelte';

  const rootState = RootState.state;
</script>

<svelte:head>
  <link rel="manifest" href="/manifest.json?locale={getLocale()}&theme={$currentColorScheme}" />
</svelte:head>

{#if $page.url.pathname.startsWith('/app/auth')}
  <slot />
{:else}
  <ResponsiveLayout>
    {#snippet desktop()}
      <DesktopLayout>
        <slot />
      </DesktopLayout>
    {/snippet}
    {#snippet mobile()}
      <MobileLayout>
        <slot />
      </MobileLayout>
    {/snippet}
  </ResponsiveLayout>

  <LogoutConfirmationDialog />
  <SettingsDialog />
{/if}

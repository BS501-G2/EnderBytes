<script lang="ts">
  import { RootState } from '$lib/states/root-state';

  import NavigationBarDesktop from './IntroNavigationBarDesktop.svelte';
  import NavigationBarMobile from './IntroNavigationBarMobile.svelte';
  import Locale, { LocaleKey } from '$lib/locale.svelte';
  import { ResponsiveLayout, Title } from '@rizzzi/svelte-commons';
</script>

<ResponsiveLayout>
  {#snippet desktop()}
    <NavigationBarDesktop />
  {/snippet}
  {#snippet mobile()}
    <NavigationBarMobile />
  {/snippet}
</ResponsiveLayout>

<svelte:head>
  <Locale string={[[LocaleKey.AppName], [LocaleKey.AppTagline]]}>
    {#snippet children([appName, appTagline])}
      <Title title={appTagline} last />
    {/snippet}
  </Locale>
</svelte:head>

<div style="display: contents; margin-top: {0}px; margin-bottom: {0}px;"></div>
<slot />

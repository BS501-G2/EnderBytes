<script lang="ts">
  import { page } from "$app/stores";
  import { goto } from "$app/navigation";

  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  import DesktopLayout from "./DesktopLayout/DesktopLayout.svelte";
  import MobileLayout from "./MobileLayout/MobileLayout.svelte";
  import { onMount } from "svelte";
  import { LocaleKey } from "$lib/locale";

  const rootState = RootState.state;

  onMount(async () => {
    $rootState.client.on("sessionTokenChange", (sessionToken) => {
      $rootState.sessionToken = sessionToken;
    });

    const sessionToken = localStorage.getItem("session-token");

    rootState.subscribe(() => {
      if ($rootState.sessionToken != null) {
        localStorage.setItem("session-token", $rootState.sessionToken);
      } else {
        localStorage.removeItem("session-token");
      }
    });

    try {
      await $rootState.client.setSessionToken(sessionToken);
    } catch {
      if (sessionToken != null) {
        localStorage.setItem("session-token", sessionToken);
      }
    }

    if ($rootState.sessionToken == null) {
      if (!$page.url.pathname.startsWith("/app/auth")) {
        goto("/app/auth", { replaceState: true });
      }
    }
  });
</script>

<svelte:head>
  <link
    rel="manifest"
    href="/api/manifest.json?locale={$rootState.locale}&theme={$rootState.theme}"
  />
  <title>{$rootState.getString(LocaleKey.AppName)}</title>
</svelte:head>

{#if $page.url.pathname.startsWith("/app/auth")}
  <slot />
{:else if $rootState.sessionToken != null}
  {#if $rootState.viewMode & ViewMode.Desktop}
    <DesktopLayout>
      <slot slot="layout-slot" />
    </DesktopLayout>
  {:else}
    <MobileLayout>
      <slot slot="layout-slot" />
    </MobileLayout>
  {/if}
{/if}

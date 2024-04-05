<script lang="ts">
  import { onMount } from "svelte";

  import { page } from "$app/stores";
  import { goto } from "$app/navigation";

  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";
  import { LocaleKey } from "$lib/locale";
  import type { Session } from "$lib/client/client";

  import DesktopLayout from "./DesktopLayout/DesktopLayout.svelte";
  import MobileLayout from "./MobileLayout/MobileLayout.svelte";

  const rootState = RootState.state;
  const appState = $rootState.appState;

  onMount(async () => {
    void (await $rootState.getClient()).on("sessionChange", (sessionToken) => {
      $rootState.sessionToken = sessionToken ?? null;
    });

    const session: Session | null = JSON.parse(
      localStorage.getItem("session") ?? "null",
    );

    rootState.subscribe(() => {
      if ($rootState.sessionToken != null) {
        localStorage.setItem(
          "session",
          JSON.stringify($rootState.sessionToken),
        );
      } else {
        localStorage.removeItem("session");
      }
    });

    try {
      if (session != null) {
        await (
          await $rootState.getClient()
        ).authenticateByToken(session.userId, session.token);
      }
    } catch {
      if (session != null) {
        localStorage.setItem("session", JSON.stringify(session));
      }
    }

    if ($rootState.sessionToken == null) {
      if (!$page.url.pathname.startsWith("/app/auth")) {
        goto("/app/auth", { replaceState: true });
      }
    }

    $appState.appTitle = $rootState.getString(LocaleKey.AppName);
  });
</script>

<svelte:head>
  <link
    rel="manifest"
    href="/api/manifest.json?locale={$rootState.locale}&theme={$rootState.theme}"
  />
  <title>{$appState.appTitle}</title>
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

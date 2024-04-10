<script lang="ts">
  import { onMount } from "svelte";

  import { page } from "$app/stores";
  import { goto } from "$app/navigation";

  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";
  import { LocaleKey } from "$lib/locale";
  import type { Client, Session } from "$lib/client/client";

  import DesktopLayout from "./Dashboard/DesktopLayout.svelte";
  import MobileLayout from "./Dashboard/MobileLayout.svelte";

  const rootState = RootState.state;
  const appState = $rootState.appState;

  export let client: Client;

  onMount(async () => {
    void (await $rootState.getClient()).on("sessionChange", (sessionToken) => {
      $rootState.sessionToken = sessionToken ?? null;
    });

    const session: Session | null = JSON.parse(
      localStorage.getItem("session") ?? "null",
    );

    let ready: boolean = false;

    rootState.subscribe(() => {
      if ($rootState.sessionToken != null) {
        localStorage.setItem(
          "session",
          JSON.stringify($rootState.sessionToken),
        );
      } else {
        localStorage.removeItem("session");

        if (ready) {
          goto("/app/auth", { replaceState: false });
        }
      }
    });

    try {
      if (session != null) {
        await (
          await $rootState.getClient()
        ).loginToken(session.userId, session.token);
      }
    } catch {
      if (session != null) {
        localStorage.setItem("session", JSON.stringify(session));
      }
    }

    ready = true;

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
    <DesktopLayout {client}>
      <slot slot="layout-slot" />
    </DesktopLayout>
  {:else}
    <MobileLayout>
      <slot slot="layout-slot" />
    </MobileLayout>
  {/if}
{/if}

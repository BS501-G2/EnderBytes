<script lang="ts">
  import { page } from "$app/stores";
  import { goto } from "$app/navigation";

  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  import DesktopLayout from "./DesktopLayout/DesktopLayout.svelte";
  import MobileLayout from "./MobileLayout/MobileLayout.svelte";
  import { onMount } from "svelte";
  import { LocaleKey } from "$lib/locale";
  import type { Session } from "$lib/client/client";

  const rootState = RootState.state;

  let size = 0;
  let sizeStr = ''

  onMount(async () => {
    (await $rootState.getClient()).on("sessionChange", (sessionToken) => {
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

    // void (async () => {
    //   while (true) {
    //     const bytes = await (
    //       await $rootState.getClient()
    //     ).randomBytes(128 * 1024);

    //     console.log(
    //       ((size) => {
    //         let a = 0;

    //         while (size > 1000) {
    //           size /= 1024;
    //           a++;
    //         }

    //         return sizeStr = `${size}${["B", "KB", "MB", "GB", "TB"][a]}`;
    //       })((size += bytes.length)),
    //     );
    //   }
    // })();

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

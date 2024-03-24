<script lang="ts" context="module">
  import { writable, type Readable, get } from "svelte/store";
  import { getContext, onMount, setContext } from "svelte";

  import { Client } from "$lib/client/client";
  import { STATE_APP, STATE_ROOT } from "$lib/values";
  import { ViewMode } from "$lib/view-mode";

  const appState = writable<AppState | null>(null);

  export class AppState {
    public static async init(): Promise<AppState> {
      {
        const cached = get(appState);

        if (cached != null) {
          return cached;
        }
      }

      const client: Client = await Client.getInstance(
        new URL("ws://localhost:8080/ws"),
      );

      return new this(client);
    }

    public constructor(client: Client) {
      this.client = client;
    }

    public client: Client;

    public async init(): Promise<void> {}
  }
</script>

<script lang="ts">
  import type { RootState } from "../+layout.svelte";
  import DesktopLayout from "./DesktopLayout/DesktopLayout.svelte";
  import MobileLayout from "./MobileLayout/MobileLayout.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);

  setContext(STATE_APP, appState);

  onMount(async () => {
    appState.set(await AppState.init());
  });

  let searchString: string = ''
</script>

<svelte:head>
  <link rel="manifest" href="/api/manifest.json?locale={$rootState.locale}" />
</svelte:head>

{#if $rootState.viewMode & ViewMode.Desktop}
  <DesktopLayout bind:searchString={searchString}>
    <slot slot="layout-slot" />
  </DesktopLayout>
{:else}
  <MobileLayout>
    <slot slot="layout-slot" />
  </MobileLayout>
{/if}

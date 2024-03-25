<script lang="ts" context="module">
  import { writable, type Readable } from "svelte/store";
  import { getContext, setContext } from "svelte";

  import { STATE_APP, STATE_ROOT } from "$lib/values";
  import { ViewMode } from "$lib/view-mode";

  export class AppSearchState {
    public constructor() {
      this.#string = "";
      this.focused = false;
      this.dismissed = false;

      this.filenameMatches = [];
      this.contentMatches = [];

      this.userMatches = [];
    }

    #string: string;
    get string(): string {
      return this.#string;
    }
    set string(searchString: string) {
      this.#string = searchString;
      this.#execute();
    }

    focused: boolean;
    dismissed: boolean;

    filenameMatches: number[];
    contentMatches: number[];
    userMatches: number[];

    get active(): boolean {
      return this.focused || (!this.dismissed && !!this.string);
    }

    #promise?: Promise<void>;

    get inProgress(): boolean {
      return !!this.#promise;
    }

    #execute() {
      this.#promise ??= (async () => {
        try {
          while (true) {
            let currentSearchString = this.string;

            try {
              await this.#getResults();
            } finally {
              await new Promise((resolve) => setTimeout(resolve, 1000));
            }

            if (this.string == currentSearchString) {
              break;
            }
          }
        } finally {
          this.#promise = undefined;
        }
      })();
    }

    async #getResults(): Promise<void> {
      await fetch("//localhost:8080/user/auth-password", {
        method: "post",

        headers: {
          "content-type": "application/json",
          authorization: "Bearer adasd",
        },

        body: JSON.stringify({
          username: `${Math.random()}`,
          password: `${Math.random()}`,
        }),
      });
    }
  }

  export class AppState {
    public constructor() {
      this.search = new AppSearchState();

      this.appInfoShown = false;
    }

    search: AppSearchState;

    appInfoShown: boolean;
  }

  const appState = writable<AppState>(new AppState());
</script>

<script lang="ts">
  import type { RootState } from "../+layout.svelte";
  import DesktopLayout from "./DesktopLayout/DesktopLayout.svelte";
  import MobileLayout from "./MobileLayout/MobileLayout.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);

  setContext(STATE_APP, appState);
</script>

<svelte:head>
  <link rel="manifest" href="/api/manifest.json?locale={$rootState.locale}" />
  <title>EnderDrive</title>
</svelte:head>

{#if $rootState.viewMode & ViewMode.Desktop}
  <DesktopLayout>
    <slot slot="layout-slot" />
  </DesktopLayout>
{:else}
  <MobileLayout>
    <slot slot="layout-slot" />
  </MobileLayout>
{/if}

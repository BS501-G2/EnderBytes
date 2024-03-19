<script lang="ts" context="module">
  import { getContext, setContext } from "svelte";
  import { type Readable, writable } from "svelte/store";

  import {
    STATE_ROOT,
    STATE_INTRO_TOP_PADDING_STATE,
    STATE_INTRO_BOTTOM_PADDING_STATE,
  } from "$lib/values";
  import { ViewMode } from "$lib/view-mode";

  const topPaddingState = writable(0);
  const bottomPaddingState = writable(0);
</script>

<script lang="ts">
  import { type RootState } from "../+layout.svelte";
  import NavigationBarDesktop from "./IntroNavigationBarDesktop.svelte";
  import NavigationBarMobile from "./IntroNavigationBarMobile.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);

  setContext(STATE_INTRO_TOP_PADDING_STATE, topPaddingState);
  setContext(STATE_INTRO_BOTTOM_PADDING_STATE, bottomPaddingState);
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <NavigationBarDesktop />
{:else if $rootState.viewMode & ViewMode.Mobile}
  <NavigationBarMobile />
{/if}

<div style="display: contents; margin-top: {0}px; margin-bottom: {0}px;"></div>
<slot />

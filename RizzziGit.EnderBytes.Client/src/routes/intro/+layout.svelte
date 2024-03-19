<script lang="ts" context="module">
  import { STATE_ROOT, STATE_INTRO_TOP_PADDING_STATE, STATE_INTRO_BOTTOM_PADDING_STATE } from "$lib/values";
  import { type RootState } from "../+layout.svelte";
  import { ViewMode } from "$lib/view-mode";
</script>

<script lang="ts">
  import { getContext, setContext } from "svelte";
  import { type Readable, readable, writable } from "svelte/store";

  import NavigationBarDesktop from "./NavigationBarDesktop.svelte";
  import NavigationBarMobile from "./NavigationBarMobile.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);

  const topPaddingState =  writable(0)
  const bottomPaddingState = writable(0)

  setContext(STATE_INTRO_TOP_PADDING_STATE, topPaddingState)
  setContext(STATE_INTRO_BOTTOM_PADDING_STATE, bottomPaddingState)
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <NavigationBarDesktop />
{:else if $rootState.viewMode & ViewMode.Mobile}
  <NavigationBarMobile />
{/if}

<div style="display: contents; margin-top: {0}px; margin-bottom: {0}px;"></div>
<slot />

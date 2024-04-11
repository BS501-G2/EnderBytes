<script lang="ts">
  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  const rootState = RootState.state;
</script>

{#if $rootState.viewMode & ViewMode.Desktop}
  <slot name="desktop" />
{:else if $rootState.viewMode & ViewMode.Mobile}
  <slot name="mobile" />
{/if}

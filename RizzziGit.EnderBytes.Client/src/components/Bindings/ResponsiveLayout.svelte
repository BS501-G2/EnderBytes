<script lang="ts">
  import { ViewMode, viewMode } from "./ResponsiveLayoutRoot.svelte";
</script>

{#if $viewMode & ViewMode.Desktop}
  <slot name="desktop" />
{:else if $viewMode & ViewMode.Mobile}
  <slot name="mobile" />
{/if}

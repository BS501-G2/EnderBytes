<script lang="ts">
  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  import DesktopLayout from "./DesktopLayout.svelte";

  const rootState = RootState.state;
  const appState = $rootState.appState;
</script>

<slot />

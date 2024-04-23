<script lang="ts">
  import { onMount } from "svelte";
  import type {
    FileBrowserFileInformation,
    FileBrowserSelection,
  } from "../../../FileBrowser.svelte";
  import type { AwaiterResetFunction } from "../../../Bindings/Awaiter.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserFileInformation;

  onMount(() => {
    $selection = [info.current.id];
  });
</script>

<style lang="scss">
</style>

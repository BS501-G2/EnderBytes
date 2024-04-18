<script lang="ts">
  import Overlay from "./Overlay.svelte";

  export let onDismiss: () => void;
  export let dim: boolean = true;
</script>

<Overlay {dim} {onDismiss}>
  <slot />
</Overlay>

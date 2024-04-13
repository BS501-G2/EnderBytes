<script lang="ts">
  import Overlay from "./Overlay.svelte";

  export let onDismiss: () => void;
  export let dim: boolean = false;
</script>

<Overlay {dim} {onDismiss}><slot /></Overlay>

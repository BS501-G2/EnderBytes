<script lang="ts">
  import Overlay from "./Overlay.svelte";

  let windowWidth: number
  let windowHeight: number

  let dialogWidth: number
  let dialogHeight: number

  export let onDismiss: () => void
  export let dim: boolean = false
</script>

<svelte:window bind:innerWidth={windowWidth} bind:innerHeight={windowHeight} />

<Overlay {dim} offsetX={(windowWidth / 2) - (dialogWidth / 2)} offsetY={(windowHeight / 2) - (dialogHeight / 2)} {onDismiss}>
  <div bind:clientWidth={dialogWidth} bind:clientHeight={dialogHeight}>
    <slot/>
  </div>
</Overlay>

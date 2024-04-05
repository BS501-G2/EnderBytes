<script lang="ts">
  import Overlay from "../../components/Overlay.svelte";

  let open: boolean = true;
  let aX: number = 0;
  let aY: number = 0;
  let a: HTMLButtonElement;
</script>

<button
  bind:this={a}
  on:click={() => {
    open = true;

    aY = a.clientTop + a.clientHeight;
    aX = a.clientLeft;

    console.log(aX, aY);
  }}
>
  <div>Test</div>
</button>

{#if open}
  <Overlay
    onDismiss={() => {
      open = false;
    }}
    offsetX={aX}
    offsetY={aY}
  >
    <div class="a">
      <p>outside</p>
      <button on:click={() => console.log("clicked inside")}
        ><p>inside</p></button
      >
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.a {
    background-color: var(--primary);
    color: var(--onPrimary);
  }
</style>

<script lang="ts">
  import type { MouseEventHandler } from "svelte/elements";

  const onClick: MouseEventHandler<HTMLButtonElement> = (event) => {
    if (element.contains(<HTMLElement>event.target)) {
      event.preventDefault();
      return;
    }

    onDismiss();
  };

  export let offsetX: number;
  export let offsetY: number;
  export let onDismiss: () => void;

  let width: number = 0;
  let height: number = 0;
  let element: HTMLElement;
</script>

<button on:click={onClick}>
  <div
    class="container"
    style="margin-left: {offsetX}px; margin-top: {offsetY}px;"
    bind:clientWidth={width}
    bind:clientHeight={height}
    bind:this={element}
  >
    <slot />
  </div>
</button>

<style lang="scss">
  button {
    position: fixed;
    left: 0px;
    top: 0px;
    width: 100vw;
    height: 100vh;

    border: none;
    background-color: transparent;
    outline: none;

    padding: 0px;

    display: flex;

    > div.container {
      display: inline-block;
    }
  }
</style>

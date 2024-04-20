<script lang="ts" context="module">
  export enum OverlayPositionType {
    Center,
    Offset,
  }

  export type OverlayPosition =
    | [type: OverlayPositionType.Center]
    | [type: OverlayPositionType.Offset, x: number, y: number];
</script>

<script lang="ts">
  import type { MouseEventHandler } from "svelte/elements";

  const onClick: MouseEventHandler<HTMLButtonElement> = (event) => {
    if (element.contains(<HTMLElement>event.target)) {
      event.preventDefault();
      return;
    }

    onDismiss();
  };

  export let dim: boolean = false;
  export let position: OverlayPosition = [OverlayPositionType.Center];
  export let onDismiss: () => void;

  let element: HTMLElement;
</script>

<div class="content">
  <div class="layer">
    <div class="view">
      <button
        on:click={onClick}
        style={dim ? "background-color: #0000003f" : ""}
      />
    </div>
  </div>
  <div class="layer">
    <div class="view">
      {#if position[0] === OverlayPositionType.Offset}
        {@const overlayPositionX = Math.abs(position[1])}
        {@const overlayPositionY = Math.abs(position[2])}

        <div class="custom-offset" style="align-items: {position[1] < 0 ? 'flex-end' : 'flex-start'}; justify-content: {position[2] < 0 ? 'flex-end' : 'flex-start'}">
          <div
            style="margin-{position[2] < 0 ? 'bottom' : 'top'}: {overlayPositionY}px; margin-{position[1] < 0 ? 'right' : 'left'}: {overlayPositionX}px;"
            class="main"
            bind:this={element}
          >
            <slot />
          </div>
        </div>
      {:else if position[0] === OverlayPositionType.Center}
        <div
          class="main"
          bind:this={element}
        >
          <slot />
        </div>
      {/if}
    </div>
  </div>
</div>

<style lang="scss">
  div.content {
    z-index: 1;

    display: flex;
    flex-direction: column;
    position: absolute;
    left: 0px;
    top: 0px;

    width: 100%;
    height: 100%;

    > div.layer {
      max-height: 0px;

      > div.view {
        min-height: 100vh;

        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;

        background-color: transparent;
        pointer-events: none;

        > * {
          pointer-events: auto;
        }

        > div.custom-offset {
          width: 100vw;
          height: 100vh;
          display: inline;
          pointer-events: none;

          display: flex;
          flex-direction: column;

          > div.main {
            pointer-events: auto;

            float: left;
          }
        }

        > button {
          width: 100vw;
          height: 100vh;
          border: none;
          background-color: transparent;
          outline: none;

          padding: 0px;

          display: flex;
        }
      }
    }
  }
</style>

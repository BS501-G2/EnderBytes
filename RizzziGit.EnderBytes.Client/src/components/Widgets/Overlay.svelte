<script lang="ts" context="module">
  export enum OverlayPositionType {
    Center,
    Position,
  }

  export type OverlayPosition =
    | [OverlayPositionType.Center]
    | [OverlayPositionType.Position, x: number, y: number];
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

  let width: number = 0;
  let height: number = 0;
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
      {#if position[0] === OverlayPositionType.Position}
        <div class="custom-offset">
          <div
            style="margin-top: {position[2]}px; margin-left: {position[1]}px;"
            class="main"
            bind:clientWidth={width}
            bind:clientHeight={height}
            bind:this={element}
          >
            <slot />
          </div>
        </div>
      {:else if position[0] === OverlayPositionType.Center}
        <div
          class="main"
          bind:clientWidth={width}
          bind:clientHeight={height}
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
    position: fixed;
    left: 0px;
    top: 0px;

    width: 100vw;
    height: 100vh;

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

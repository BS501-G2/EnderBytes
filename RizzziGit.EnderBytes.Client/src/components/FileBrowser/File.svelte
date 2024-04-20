<script lang="ts">
  import { goto } from "$app/navigation";
  import { hasKeys } from "../Bindings/Keyboard.svelte";

  export let file: any;
  export let selected: boolean = false;

  export let onClick: () => void;

  let hovered: boolean = false;
</script>

<button
  class="file-entry {selected ? 'selected' : ''}"
  on:pointerenter={() => (hovered = true)}
  on:pointerleave={() => (hovered = false)}
  on:click={onClick}
  on:dblclick={() => {
    const path = "/app/files/" + file.id;

    if (hasKeys("control")) {
      window.open(path, "_blank");
    } else {
      goto(path);
    }
  }}
>
  <div class="overlay">
    {#if selected || hovered}
      <input type="checkbox" disabled checked={selected} on:click={onClick} />
    {/if}
  </div>
  <div class="base">
    <div class="file-preview">
      <img class="file-preview" src="/favicon.svg" alt="asd" />
    </div>
    <div class="file-info">
      <span class="file-name">
        {file.name}
      </span>
    </div>
  </div>
</button>

<style lang="scss">
  button.file-entry:hover {
    > div.base {
      > div.file-info {
        > span.file-name {
          text-decoration: underline;
        }
      }
    }
  }

  button.file-entry {
    background-color: var(--backgroundVariant);
    border: solid 1px transparent;
    cursor: pointer;

    padding: 8px;

    display: flex;
    flex-direction: column;

    border-radius: 8px;

    > div.overlay {
      width: 100%;
      height: 0px;
      z-index: 0;

      display: flex;
      flex-direction: column;
      align-items: last baseline;
    }

    > div.base {
      > div.file-preview {
        max-width: 128px;
        max-height: 128px;
        min-width: 128px;
        min-height: 128px;

        padding: 8px;
        box-sizing: border-box;

        > img {
          width: 100%;
          height: 100%;
        }
      }

      > div.file-info {
        width: 128px;
        min-height: 1em;
        display: flex;
        flex-direction: row;

        > span.file-name {
          font-weight: bold;

          text-align: center;
          text-overflow: ellipsis;

          overflow: hidden;
          white-space: nowrap;

          flex-grow: 1;
        }
      }
    }
  }

  button.file-entry.selected {
    border-color: var(--primaryContainer);
    background-color: var(--primary);
    color: var(--onPrimary);
  }

  button.file-entry:hover {
    border-color: var(--primary);
  }
</style>

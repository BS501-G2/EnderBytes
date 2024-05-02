<script lang="ts">
  import { goto } from "$app/navigation";
  import { FolderIcon } from "svelte-feather-icons";
  import { hasKeys } from "../Bindings/Keyboard.svelte";

  export let file: any;
  export let selected: boolean = false;

  export let onClick: () => void;

  let hovered: boolean = false;

  let button: HTMLElement;
  let link: HTMLAnchorElement;
</script>

<a
  bind:this={link}
  href="/app/files/{file.id}"

  on:click|preventDefault={() => {}}
>
  <button
    bind:this={button}
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
        {#if file.isFolder}
          <FolderIcon size="100%" strokeWidth={0.5} />
        {:else}
          <img class="file-preview" src="/favicon.svg" alt="asd" />
        {/if}
      </div>
      <div class="file-info">
        <span class="file-name">
          {file.name}
        </span>
      </div>
    </div>
  </button>
</a>

<style lang="scss">
  a {
    text-decoration: none;
    color: inherit;
  }

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
    color: var(--onBackgroundVariant);
    border: solid 1px transparent;
    cursor: pointer;

    padding: 8px;

    display: flex;
    flex-direction: column;

    border-radius: 0.5em;

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
        max-width: 96px;
        max-height: 96px;
        min-width: 96px;
        min-height: 96px;

        padding: 8px;
        box-sizing: border-box;

        > img.file-preview {
          width: 100%;
          height: 100%;
        }
      }

      > div.file-info {
        width: 96px;
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

<script lang="ts">
  export let name: string;
  export let selected: boolean | null = null;

  let hovered: boolean = false;

  function onClick() {
    if (selected == null) {
      return;
    }

    selected = !selected;
  }
</script>

<button
  class="file-entry"
  on:pointerenter={() => (hovered = true)}
  on:pointerleave={() => (hovered = false)}
  on:click={onClick}
>
  <div class="overlay">
    {#if selected || hovered}
      <input type="checkbox" disabled checked={selected ?? false} />
    {/if}
  </div>
  <div class="base">
    <img class="file-preview" src="/favicon.svg" alt="asd" />
    <div class="file-info">
      <span class="file-name" bind:textContent={name} contenteditable="false" />
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

    > div.overlay {
      width: 100%;
      height: 0px;
      z-index: 0;

      display: flex;
      flex-direction: column;
      align-items: last baseline;
    }

    > div.base {
      > img.file-preview {
        width: 128px;
        height: 128px;

        box-sizing: border-box;
      }

      > div.file-info {
        width: 128px;
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

  button.file-entry:hover {
    border-color: var(--primary);
  }
</style>

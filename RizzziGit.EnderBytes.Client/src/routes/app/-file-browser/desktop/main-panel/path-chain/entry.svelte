<script lang="ts">
  import type { File } from '$lib/server/db/file';
    import { FileType } from '$lib/shared/db';

  const {
    file,
    forward = false,
    onMenu,
    onClick
  }: {
    file: File;
    forward?: boolean;
    onMenu: (event: MouseEvent) => void;
    onClick: (event: MouseEvent) => void;
  } = $props();
</script>

<div class="path-chain-entry">
  {#snippet link()}
    <button class="link {forward ? 'forward' : ''}" title={file.name} onclick={onClick}>
      {#snippet icon()}
        <i class="fa-solid fa-{file.type === FileType.Folder ? 'folder' : 'file'}"></i>
      {/snippet}
      {#if forward}
        {@render icon()}
      {/if}

      <p class="path-chain-string">{file.name}</p>
    </button>
  {/snippet}

  {#snippet expand()}
    <button class="menu-button {forward ? 'forward' : ''}" onclick={onMenu}>
      <i class="fa-solid fa-chevron-right"></i>
    </button>
  {/snippet}

  {#if forward}
    {@render link()}
    {#if file.type === FileType.Folder}
      {@render expand()}
    {/if}
  {:else}
    {@render expand()}
    {@render link()}
  {/if}
</div>

<style lang="scss">
  p.path-chain-string {
    max-width: 128px;

    text-overflow: ellipsis;
    text-wrap: nowrap;
    overflow: hidden;
  }

  div.path-chain-entry {
    display: flex;
    flex-direction: row;

    color: var(--onBackgroundVariant);

    max-width: 256px;

    border-radius: 8px;
    transition: all linear 150ms;
    border: 1px solid transparent;

    button.link {
      display: flex;
      flex-direction: row;
      justify-content: start;
      gap: 8px;
      border: 1px solid transparent;

      > i {
        font-size: 1.25em;
        min-height: 16px;
        max-height: 16px;
        aspect-ratio: 1;
      }
    }

    > button {
      min-width: 0px;
      background-color: unset;
      border: unset;

      padding: 4px 8px;

      color: inherit;
      transition: all linear 150ms;

      text-overflow: ellipsis;
      text-wrap: nowrap;

      display: flex;
      flex-direction: column;
      align-items: left;
      justify-content: center;

      cursor: pointer;
    }

    > button:hover {
      background-color: var(--background);
      color: var(--onBackground);
    }

    > button.menu-button {
      border-radius: 8px 0px 0px 8px;

      i {
        font-size: 1.25em;
        font-weight: light;
      }
    }

    > button.link {
      border-radius: 0px 8px 8px 0px;
      flex-grow: 1;

      > p {
        min-height: 1em;
        overflow: hidden;

        text-align: left;
        text-wrap: nowrap;
        text-overflow: ellipsis;
      }
    }

    > button.menu-button.forward {
      border-radius: 0px 8px 8px 0px;
    }

    > button.link.forward {
      border-radius: 8px 0px 0px 8px;
    }
  }

  div.path-chain-entry:hover {
    border-color: var(--background);
  }
</style>

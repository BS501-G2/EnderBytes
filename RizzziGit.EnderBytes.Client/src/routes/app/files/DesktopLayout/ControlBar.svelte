<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import { PlusCircleIcon, ScissorsIcon, CopyIcon, ClipboardIcon } from "svelte-feather-icons";

  const rootState = RootState.state;
  const appState = $rootState.appState;
  const fileState = $appState.fileState;
</script>

<div class="controls">
  <div class="button" title="New Item">
    <button>
      <PlusCircleIcon />
      <p>New Item</p>
    </button>
  </div>
  <div class="divider"></div>
  <div class="button" title="Cut">
    <button>
      <ScissorsIcon />
      <p>Cut</p>
    </button>
  </div>
  <div class="button" title="Copy">
    <button>
      <CopyIcon />
      <p>Copy</p>
    </button>
  </div>
  <div class="button" title="Paste">
    <button>
      <ClipboardIcon />
      <p>Paste</p>
    </button>
  </div>
</div>

<style lang="scss">
  div.controls {
    padding: 16px;

    background-color: var(--background);

    display: flex;
    flex-direction: row;
    gap: 8px;

    > div.divider {
      width: 1px;
      background-color: var(--onBackground);
      margin: 4px 0px 4px 0px;
    }

    > div.button {
      > button {
        cursor: pointer;

        background-color: unset;
        color: var(--onBackground);

        display: flex;
        align-items: center;
        justify-content: center;
        flex-wrap: wrap;

        gap: 8px;

        border-width: 1px;
        border-color: transparent;
        border-radius: 8px;

        outline: none;

        padding: 8px;

        transition: all linear 150ms;

        > p {
          margin: 0px;
        }
      }

      > button:hover {
        border-color: var(--primary);
      }

      > button:disabled {
        color: gray;
        border-color: transparent;
      }

      > button:active {
        background-color: var(--primary);
        color: var(--onPrimary);
      }
    }
  }
</style>

<script lang="ts">
  import { goto } from "$app/navigation";
  import { FolderIcon, ChevronRightIcon } from "svelte-feather-icons";
  import Awaiter from "../../../Bindings/Awaiter.svelte";
  import Overlay, {
    OverlayPositionType,
  } from "../../../Widgets/Overlay.svelte";
  import LoadingSpinner from "../../../Widgets/LoadingSpinner.svelte";
  import { fetchAndInterpret } from "../../../Bindings/Client.svelte";

  export let files: any[];
  export let index: number;

  let menuButton: HTMLButtonElement;

  let menuOffsetX: number = 0;
  let menuOffsetY: number = 0;
  let menuShown: boolean = false;

  function updateMenuDimensions() {
    if (menuButton == null) {
      return;
    }

    let offsetLeft = 0;

    let elem: HTMLElement | null = menuButton;

    while (elem != null) {
      offsetLeft += elem.offsetLeft;

      elem = elem.parentElement;
    }

    menuOffsetX =
      menuButton.offsetLeft +
      menuButton.clientWidth -
      (menuButton.parentElement?.parentElement?.scrollLeft ?? 0);
    menuOffsetY = menuButton.offsetTop - 4;
  }

  $: file = files[index];
</script>

<svelte:window on:resize={updateMenuDimensions} />

<div class="path-entry">
  {#if files[index] == null}
    <button on:click={() => goto(`/app/files`)}>
      <p>/</p>
    </button>
  {:else}
    <button on:click={() => goto(`/app/files/${file.id}`)}>
      <p>{file.name}</p>
    </button>
  {/if}
  {#if index < files.length - 1}
    <button
      bind:this={menuButton}
      on:scroll={updateMenuDimensions}
      on:click={() => {
        menuShown = true;
        updateMenuDimensions();
      }}><ChevronRightIcon strokeWidth={1} size="20em" /></button
    >
  {/if}
</div>

{#if menuShown}
  <Overlay
    onDismiss={() => (menuShown = false)}
    position={[OverlayPositionType.Position, menuOffsetX, menuOffsetY]}
  >
    <div class="menu">
      <Awaiter
        callback={() =>
          fetchAndInterpret(
            `/file/${file != null ? `:${file.id}` : "!root"}/files`,
          )}
      >
        <svelte:fragment slot="loading">
          <div class="loading-icon">
            <LoadingSpinner />
          </div>
        </svelte:fragment>
        <svelte:fragment slot="success" let:result={files}>
          <div class="file-list">
            {#each files as fileEntry}
              <button
                on:click={() => {
                  menuShown = false;
                  goto(`/app/files/${fileEntry.id}`);
                }}
              >
                <FolderIcon size="18em" />
                <p>
                  {fileEntry.name}
                </p>
              </button>
            {/each}
          </div>
        </svelte:fragment>
      </Awaiter>
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.path-entry {
    display: flex;

    align-items: center;

    height: 100%;
  }

  button {
    background-color: transparent;
    color: var(--primary);
    // margin: 4px 0px 4px 0px;

    height: calc(100% - 4px);

    cursor: pointer;

    border-style: solid;
    border-width: 1px;
    border-color: transparent;
    border-radius: 8px;
    user-select: none;

    > p {
      margin: 4px 2px 4px 2px;

      font-style: italic;
    }

    display: flex;
    gap: 8px;
    text-align: start;
    align-items: center;
  }

  button:hover {
    border-color: var(--primary);
  }

  button:active {
    border-color: transparent;
    background: var(--primary);
    color: var(--onPrimary);
    border-color: var(--primary);
  }

  div.menu {
    display: flex;
    flex-direction: column;

    background-color: var(--backgroundVariant);

    padding: 4px;
    border-radius: 8px;
    // border-style: solid;
    // border-width: 1px;
    // border-color: var(--primary);
    box-shadow: 1px 1px 8px rgba(0, 0, 0, 0.25);

    > div.loading-icon {
      max-width: 72px;
      max-height: 72px;
      min-width: 72px;
      min-height: 72px;

      box-sizing: border-box;
      padding: 16px;
    }

    > div.file-list {
      display: flex;
      flex-direction: column;
    }
  }
</style>

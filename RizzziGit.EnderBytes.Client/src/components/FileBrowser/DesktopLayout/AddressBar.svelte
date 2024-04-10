<script lang="ts">
  import { FolderIcon } from "svelte-feather-icons";

  import { RootState } from "$lib/states/root-state";

  import Overlay from "../../Overlay.svelte";
    import type { Client } from "$lib/client/client";

  export let client: Client
  export let currentFileId: number | null

  const rootState = RootState.state;

  const strings: string[] = [
    "Root",
    "School",
    "Test",
    "School",
    "Test",
    "School",
    "Test",
    "School",
    "Test",
    "School",
    "Test",
    "School",
    "Test",
  ];

  let menuOffsetX: number = 0;
  let menuOffsetY: number = 0;
  let menuOpen: boolean = false;
</script>

<div class="address">
  <div class="address-content">
    <FolderIcon />
    {#each strings as string}
      <div class="path-entry">
        <button
          on:click={(e) => {
            menuOffsetX =
              e.currentTarget.offsetLeft + e.currentTarget.clientWidth - (e.currentTarget.parentElement?.parentElement?.scrollLeft ?? 0);
            menuOffsetY = e.currentTarget.offsetTop - 5;
            menuOpen = true;
          }}><p>></p></button
        >

        <button><p>{string}</p></button>
      </div>
    {/each}
  </div>

  {#if menuOpen}
    <Overlay
      offsetX={menuOffsetX}
      offsetY={menuOffsetY}
      onDismiss={() => (menuOpen = false)}
    >
      <div class="menu">
        <button
          ><FolderIcon />
          <p>asd</p></button
        >
        <button
          ><FolderIcon />
          <p>asd asdf</p></button
        >
        <button
          ><FolderIcon />
          <p>asda sdf</p></button
        >
        <button
          ><FolderIcon />
          <p>asda sdf asdf</p></button
        >
      </div></Overlay
    >{/if}
</div>

<style lang="scss">
  div.address {
    background-color: var(--primaryContainer);
    color: var(--primary);
    padding: 8px;

    > div.address-content {
      background-color: var(--background);

      display: flex;
      align-items: center;
      padding: 0px 8px 0px 8px;

      border-style: solid;
      border-width: 1px;
      border-color: var(--primary);

      max-width: max(75%, 640px);

      margin: auto;

      overflow: auto;

      > div.path-entry {
        display: flex;
      }
    }

    button {
      background-color: transparent;
      color: var(--primary);
      margin: 4px 0px 4px 0px;

      cursor: pointer;

      border-style: solid;
      border-width: 1px;
      border-color: transparent;
      // border-radius: 8px;
      user-select: none;

      > p {
        margin: 4px 2px 4px 2px;
      }

      display: flex;
      gap: 8px;
    }

    button:hover {
      border-color: var(--onPrimaryContainer);
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

      background-color: var(--background);

      padding: 4px;
      // border-radius: 8px;
      border-style: solid;
      border-width: 1px;
      border-color: var(--primary);

      > button {
        text-align: start;
      }
    }
  }
</style>

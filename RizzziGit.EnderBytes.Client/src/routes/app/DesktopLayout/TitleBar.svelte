<script lang="ts">
  import { SearchIcon } from "svelte-feather-icons";
  import { strings } from "$lib/locale";
  import { onDestroy, onMount } from "svelte";

  export let searchString: string;

  let searchElement: HTMLDivElement | null = null;
  let searchResultElement: HTMLDivElement | null = null;

  let active: boolean = false;

  function updateFocus() {
    active = document.activeElement == searchElement?.children[1];
  }

  onMount(() => {
    searchElement?.children[1].addEventListener("focusin", updateFocus);
    searchElement?.children[1].addEventListener("focusout", updateFocus);
  });

  onDestroy(() => {
    searchElement?.children[1].removeEventListener("focusin", updateFocus);
    searchElement?.children[1].removeEventListener("focusout", updateFocus);
  });
</script>

<!-- <input type="text" bind:value={searchString} placeholder="Search..." /> -->

<div class="">asd</div>

<div class="search-container">
  <div class="search-background">
    <div class="search" bind:this={searchElement}>
      <SearchIcon size="16" strokeWidth={2} />
      <input bind:value={searchString} />
    </div>
  </div>
</div>

{#if searchString && active}
  <div
    bind:this={searchResultElement}
    class="search-results"
    style="left: {searchElement?.offsetLeft}px; top: {searchElement?.offsetTop +
      searchElement?.offsetHeight}px; width: {searchElement?.offsetWidth}px;"
  ></div>
{/if}

<style lang="scss">
  div.search-results {
    position: fixed;
    background-color: var(--primaryContainer);

    left: 0px;
    top: 0px;

    min-height: 64px;
    max-height: 90vh;
  }

  div.search-container {
    padding: 8px;

    display: flex;

    flex-grow: 1;

    > div.search-background {
      width: 100%;

      max-width: min(640px, 50%);
      height: 100%;
      box-sizing: border-box;

      margin: 0px auto 0px auto;

      background-color: white;
      border-radius: 4px;

      > div.search {
        app-region: no-drag;
        background-color: var(--background);
        color: var(--primary);

        width: 100%;
        height: 100%;
        box-sizing: border-box;

        padding-left: 8px;
        padding-right: 8px;

        border-style: solid;
        border-width: 1px;
        border-radius: 4px;

        display: flex;

        justify-items: center;
        flex-direction: row;
        flex-wrap: nowrap;
        align-items: center;

        gap: 8px;

        > input {
          border: none;
          outline-width: 0px;

          flex-grow: 1;
          height: 100%;

          background-color: unset;

          color: unset;
          font-size: 18px;
        }

        > input:focus,
        > input:active {
          div.search {
            border-color: white;
          }
        }
      }
    }
  }

  // input {
  //   app-region: no-drag;

  //   background-color: var(--secondary);
  //   color: var(--onSecondary);

  //   padding: 8px 16px 8px 16px;
  //   margin: 4px;

  //   border-style: solid;
  //   border-color: var(--onSecondary);
  //   border-width: 1px;
  //   border-radius: 32px;
  // }

  // input:hover {
  //   border-color: white;
  // }
</style>

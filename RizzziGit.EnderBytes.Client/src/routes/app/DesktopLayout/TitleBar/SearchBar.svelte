<script lang="ts" context="module">
  import { getContext } from "svelte";
  import type { RootState } from "../../../+layout.svelte";
  import { STATE_APP, STATE_ROOT } from "$lib/values";
  import type { Readable, Writable } from "svelte/store";
  import type { AppState } from "../../+layout.svelte";
</script>

<script lang="ts">
  import { SearchIcon } from "svelte-feather-icons";
  import { LocaleKey } from "$lib/locale";

  const rootState = getContext<Writable<RootState>>(STATE_ROOT);
  const appState = getContext<Writable<AppState>>(STATE_APP);

  let searchElement: HTMLDivElement | null = null;

  function updateFocus() {
    if (
      ($appState.search.focused =
        document.activeElement == searchElement?.children[1])
    ) {
      $appState.search.dismissed = false;
    }
  }
</script>

{#if $appState.search.active}
  <div
    class="search-results-container"
    style="left: {(searchElement?.offsetLeft ?? 0) -
      4}px; top: {(searchElement?.offsetTop ?? 0) -
      4}px; width: {(searchElement?.offsetWidth ?? 0) + 8}px;"
  >
    {#if $appState.search.string}
      <div class="search-results">
        <div><b>Search Results</b></div>
      </div>
    {:else}
      <div class="empty-search-string">
        <SearchIcon size="16" strokeWidth={2} />
        <p>{$rootState.getString(LocaleKey.SearchBannerPlaceholderText)}</p>
      </div>
    {/if}
  </div>
{/if}

<div class="search-container">
  <div
    class="search-background"
    style={$appState.search.active ? "z-index: 0;" : ""}
  >
    <div class="search" bind:this={searchElement}>
      <SearchIcon size="16" strokeWidth={2} />
      <input
        bind:value={$appState.search.string}
        on:focusin={updateFocus}
        on:focusout={updateFocus}
        placeholder={$rootState.getString(LocaleKey.SearchBarPlaceholder)}
      />
    </div>
  </div>
</div>

<style lang="scss">
  div.search-results-container {
    position: fixed;
    background-color: var(--primary);

    color: var(--primaryVariant);

    min-height: 64px;
    max-height: 90vh;

    border-radius: 4px;

    padding: 48px 8px 8px 8px;
    box-sizing: border-box;

    > div.empty-search-string {
      width: 100%;

      display: flex;

      align-items: center;
      justify-content: center;

      gap: 8px;
    }
  }

  div.search-container {
    margin: 8px;

    display: flex;

    flex-grow: 1;

    > div.search-background {
      width: 100%;

      max-width: min(640px, 50%);
      height: 100%;
      box-sizing: border-box;

      @media only screen and (max-width: 960px) {
        max-width: 100%;
      }

      margin: 0px auto 0px auto;

      background-color: white;
      border-radius: 4px;

      > div.search {
        -webkit-app-region: no-drag;
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
          outline: none;

          flex-grow: 1;
          height: 100%;

          background-color: unset;
          color: var(--onPrimary);

          color: unset;
          font-size: 14px;
        }

        > input::placeholder {
          color: var(--primary);

          font-style: italic;
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
</style>

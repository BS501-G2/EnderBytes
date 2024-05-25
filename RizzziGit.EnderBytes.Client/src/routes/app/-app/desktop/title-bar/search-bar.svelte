<script lang="ts">
  import { SearchIcon } from 'svelte-feather-icons';

  import { RootState } from '$lib/states/root-state';
  import Locale, { LocaleKey, getDerivedString } from '$lib/locale.svelte';

  const rootState = RootState.state;
  const appState = $rootState.appState;
  const searchState = $appState.searchState;

  let searchElement: HTMLDivElement | null = null;

  const string_SearchBarPlaceHolder = getDerivedString(LocaleKey.SearchBarPlaceholder);

  function updateFocus() {
    if (($searchState.focused = document.activeElement == searchElement?.children[1])) {
      $searchState.dismissed = false;
    }
  }
</script>

{#if $searchState.active}
  <div
    class="search-results-container"
    style="left: {(searchElement?.offsetLeft ?? 0) - 4}px; top: {(searchElement?.offsetTop ?? 0) -
      4}px; width: {(searchElement?.offsetWidth ?? 0) + 8}px;"
  >
    {#if $searchState.string}
      <div class="search-results">
        <div><b>Search Results</b></div>
      </div>
    {:else}
      <div class="empty-search-string">
        <SearchIcon size="16" strokeWidth={2} />
        <p>
          <Locale string={[[LocaleKey.SearchBannerPlaceholderText]]} />
        </p>
      </div>
    {/if}
  </div>
{/if}

<div class="search-container">
  <div class="search-background" style={$searchState.active ? 'z-index: 0;' : ''}>
    <div class="search" bind:this={searchElement}>
      <SearchIcon size="16" strokeWidth={2} />
      <input
        bind:value={$searchState.string}
        on:focusin={updateFocus}
        on:focusout={updateFocus}
        placeholder={$string_SearchBarPlaceHolder}
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
    // margin: 8px;

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
      }
    }
  }
</style>

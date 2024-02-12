<script lang="ts">
  import { MenuIcon } from "svelte-feather-icons";

  import { goto } from "$app/navigation";

  import type { NavigationMenuItem } from "$lib/navigation-menu-items";
  import { DisplayMode } from "$lib/display-mode";

  export let menuItems: NavigationMenuItem[];

  let isMenuVisible = false;

  const onMenuClick = () => (isMenuVisible = !isMenuVisible);

  const onMenuEntryClick = (path: string) => {
    onMenuClick();
    goto(path);
  };

  export let displayMode: DisplayMode;

  let clientScroll: number;

  $: isNavTransparent = !(
    (displayMode & DisplayMode.Mobile && isMenuVisible) ||
    clientScroll != 0
  );
</script>

<svelte:window bind:scrollY={clientScroll} />

<div class="navigation-bar {isNavTransparent ? 'transparent' : ''}">
  {#if displayMode & DisplayMode.Mobile}
    <div class="site-logo"></div>
    <div
      class="mobile-menu-icon"
      on:keydown
      role="button"
      on:click={onMenuClick}
      tabindex="0"
    >
      <MenuIcon />
    </div>
  {:else}
    <div class="navigation-container">
      <div class="site-logo"></div>
      <ul class="navigation-list">
        {#each menuItems as [label, path]}
          <div
            class="navigation-entry"
            on:keydown
            role="button"
            on:click={onMenuEntryClick.bind(null, path)}
            tabindex="0"
          >
            <li><p contenteditable="false" bind:textContent={label}></p></li>
          </div>
        {/each}
      </ul>
    </div>
    <!-- <div class="navigation-elements"></div> -->
  {/if}
</div>

{#if displayMode & DisplayMode.Mobile}
  <div class="mobile-navigation-container {isMenuVisible ? '' : 'hidden'}">
    <ul>
      {#each menuItems as [label, path]}
        <div
          class="mobile-navigation-entry"
          on:keydown
          role="button"
          on:click={onMenuEntryClick.bind(null, path)}
          tabindex="0"
        >
          <li>
            <p contenteditable="false" bind:textContent={label}></p>
          </li>
        </div>
      {/each}
    </ul>
  </div>
{/if}

<style lang="scss">
  div.navigation-bar.transparent {
    background-color: transparent;
    background-position-y: 0px;
  }

  div.navigation-bar {
    background-color: var(--highlight-background-color-01);
    background-image: linear-gradient(
      to bottom,
      rgba(0, 0, 0, 0.5),
      rgba(255, 255, 255, 0)
    );
    background-position-y: -64px;
    background-repeat: no-repeat;

    transition-duration: 300ms;
    transition-timing-function: ease-in-out;

    position: fixed;
    left: 0px;
    top: 0px;

    width: 100%;
    height: 64px;

    color: white;

    div.navigation-container {
      max-width: 1280px;

      margin: auto;

      display: flex;
    }

    div.site-logo,
    div.navigation-container > div.site-logo {
      width: 48px;
      height: 48px;

      margin: 8px;

      box-sizing: border-box;

      background-image: url("/favicon.png");
      background-repeat: no-repeat;
      background-size: contain;

      image-rendering: pixelated;
    }

    div.mobile-menu-icon {
      width: 48px;
      height: 48px;

      margin: 8px;

      box-sizing: border-box;

      display: flex;
      flex-wrap: nowrap;
      justify-content: space-evenly;
      align-items: center;
    }

    div.navigation-container {
      ul {
        margin: unset;
        list-style: none;
        padding: unset;

        div.navigation-entry {
          cursor: pointer;

          user-select: none;

          height: 64px;

          margin: 0px 8px 0px 8px;
          padding: 0px 8px 0px 8px;

          display: inline-flex;
          flex-wrap: nowrap;
          align-content: center;
          justify-content: space-evenly;
          align-items: center;
        }
      }
    }
  }

  @media only screen and (max-width: 720px) {
    div.navigation-bar {
      display: flex;
      flex-wrap: nowrap;
      flex-direction: row;
      align-items: center;
      justify-content: space-between;

      div.navigation-container {
        display: none;
      }
    }

    div.mobile-navigation-container.hidden {
      opacity: 0;
      pointer-events: none;
    }

    div.mobile-navigation-container {
      background-color: var(--highlight-background-color-01);

      position: fixed;
      left: 0px;
      top: 64px;

      width: 100%;

      color: white;
      transition-duration: 300ms;
      transition-timing-function: ease-in-out;
      opacity: 1;

      ul {
        list-style: none;
        padding: 0px;

        div.mobile-navigation-entry:first-child {
          border-top: 1px solid white;
        }

        div.mobile-navigation-entry {
          cursor: pointer;

          width: calc(100% - 64px);
          height: min-content;
          margin: auto;

          user-select: none;

          border-bottom: 1px solid white;
        }
      }
    }
  }
</style>

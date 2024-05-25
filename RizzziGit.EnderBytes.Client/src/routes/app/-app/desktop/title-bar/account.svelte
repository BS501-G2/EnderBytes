<script lang="ts">
  import { ChevronDownIcon } from 'svelte-feather-icons';
  import TitleBarChip from './chip.svelte';
  import TitleBarButton from './chip/button.svelte';
  import { enabled as accountSettingsDialog } from '../../account-settings-dialog.svelte';
  import { enabled as logoutConfirmationDialog } from '../../logout-confirmation-dialog.svelte';
  import { enabled as appSettingsDialog } from '../../app-settings-dialog.svelte';
  import { goto } from '$app/navigation';
  import { Overlay, OverlayPositionType } from '@rizzzi/svelte-commons';
  import { settingsDialogState, showSettingsDialog } from '../../../settings-dialog.svelte';

  let menuLocationAnchor: HTMLElement;
  let menuX = 0;
  let menuY = 0;
  let menuOpen = false;

  function updateMenuLocation() {
    menuX = window.innerWidth - menuLocationAnchor.offsetLeft - menuLocationAnchor.clientWidth - 16;
    menuY = menuLocationAnchor.offsetTop + menuLocationAnchor.clientHeight + 8;
  }

  function onClick(func: () => void) {
    func();
    menuOpen = false;
  }
</script>

<svelte:window on:resize={updateMenuLocation} />

{#if menuOpen}
  <Overlay
    position={[OverlayPositionType.Offset, -menuX, menuY]}
    onDismiss={() => (menuOpen = false)}
  >
    <div class="account-info"></div>
    <div class="account-menu">
      <a href="/app/users?id=!me" on:click={() => (menuOpen = false)}> View Profile </a>
      <button on:click={() => onClick(() => showSettingsDialog())}> App Settings </button>
      <div class="divider"></div>
      <button class="logout" on:click={() => onClick(() => ($logoutConfirmationDialog = true))}
        >Logout</button
      >
    </div>
  </Overlay>
{/if}

<TitleBarChip>
  <TitleBarButton
    onClick={() => {
      menuOpen = true;
      updateMenuLocation();
    }}
  >
    <div class="container">
      <img bind:this={menuLocationAnchor} alt="User's Avatar" src="/favicon.svg" />
      <ChevronDownIcon size="10em" />
    </div>
  </TitleBarButton>
</TitleBarChip>

<style lang="scss">
  div.account-menu {
    background-color: var(--primary);
    color: var(--onPrimary);

    display: flex;
    flex-direction: column;

    box-sizing: border-box;

    gap: 4px;
    padding: 8px;

    box-shadow: 2px 2px 8px #0000007f;

    border-radius: 8px;

    > button,
    a {
      padding: 8px;

      text-align: right;

      background-color: transparent;
      color: var(--onPrimary);
      text-decoration: none;
      border: none;
      font-size: small;

      cursor: pointer;

      border-radius: 8px;
    }

    > button:hover,
    a:hover {
      background-color: #ffffff7f;
    }

    > button:active,
    a:active {
      background: var(--backgroundVariant);
      color: var(--onBackground);
    }

    > div.divider {
      min-height: 1px;
      max-height: 1px;

      margin: 0px 8px 0px 8px;

      background-color: var(--onPrimary);
    }

    > button.logout {
      color: var(--error);
    }
  }

  div.container {
    display: flex;
    align-items: center;

    > img {
      width: 32px;
      height: 32px;
    }
  }
</style>

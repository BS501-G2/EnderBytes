<script lang="ts">
  import { ChevronDownIcon } from "svelte-feather-icons";
  import TitleBarChip from "./TitleBarChip.svelte";
  import TitleBarButton from "./TitleBarChip/Button.svelte";
  import Overlay, {
    OverlayPositionType,
  } from "../../../Widgets/Overlay.svelte";
  import { enabled as accountSettingsDialog } from "../../AccountSettingsDialog.svelte";
  import { enabled as logoutConfirmationDialog } from "../../LogoutConfirmationDialog.svelte";
  import { goto } from "$app/navigation";

  let menuLocationAnchor: HTMLElement;
  let menuX = 0;
  let menuY = 0;
  let menuOpen = false;

  function updateMenuLocation() {
    menuX = (window.innerWidth - menuLocationAnchor.offsetLeft) - menuLocationAnchor.clientWidth - 16;
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
    position={[OverlayPositionType.Offset, - menuX, menuY]}
    onDismiss={() => (menuOpen = false)}
  >
    <div class="account-menu">
      <button on:click={() => onClick(() => goto("/app/users/!me"))}
        >View Profile</button
      >
      <button on:click={() => onClick(() => ($accountSettingsDialog = true))}
        >Account Settings</button
      >
      <div class="divider" />
      <button class="logout" on:click={() => onClick(() => $logoutConfirmationDialog = true)}>Logout</button>
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
      <img
        bind:this={menuLocationAnchor}
        alt="User's Avatar"
        src="/favicon.svg"
      />
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

    > button {
      padding: 8px;

      text-align: right;

      background-color: transparent;
      color: var(--onPrimary);
      border: none;

      cursor: pointer;
      user-select: none;

      border-radius: 8px;
    }

    > button:hover {
      background-color: #ffffff7f;
    }

    > button:active {
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

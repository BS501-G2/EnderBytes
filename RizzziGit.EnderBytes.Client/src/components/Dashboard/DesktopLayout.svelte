<script lang="ts" context="module">
  enum ActionTab {
    Operations,
    Notification,
  }
</script>

<script lang="ts">

  import { BellIcon, CheckSquareIcon } from "svelte-feather-icons";

  import LeftPanelNavigationBar from "./DesktopLayout/LeftPanelNavigationBar.svelte";
  import LeftPanelAccountBar from "./DesktopLayout/LeftPanelAccountBar.svelte";
  import TitleBar from "./DesktopLayout/TitleBar.svelte";
  import Keyboard from "../Bindings/Keyboard.svelte";
  import OperationsTab from "./DesktopLayout/OperationsTab.svelte";
  import NotificationsTab from "./DesktopLayout/NotificationsTab.svelte";
  import BackgroundTaskList, {
    pendingTasks,
  } from "../BackgroundTaskList.svelte";

  export let accountSettingsDialog: boolean

  let actionTab: ActionTab | null = null;

  function onUnload() {
    if ($pendingTasks.length > 0) {
      actionTab = ActionTab.Operations;
    }
  }
</script>

<svelte:window on:beforeunload={onUnload} />

<Keyboard />

<div class="viewport">
  <TitleBar></TitleBar>

  <div class="panel-container">
    <div class="panel left-panel">
      {#if actionTab === null}
        <LeftPanelNavigationBar />
        {#if $pendingTasks.length}
          <div class="divider" />
          <BackgroundTaskList />
        {/if}
        <div class="divider" />
        <div class="actions">
          <button on:click={() => (actionTab = ActionTab.Notification)}>
            <p>0</p>
            <BellIcon />
          </button>
          <button on:click={() => (actionTab = ActionTab.Operations)}>
            <p>
              {$pendingTasks.length}
            </p>
            <CheckSquareIcon />
          </button>
        </div>
      {:else if actionTab === ActionTab.Operations}
        <OperationsTab
          onDismiss={() => {
            actionTab = null;
          }}
        />
      {:else if actionTab === ActionTab.Notification}
        <slot name="notification" />
        <NotificationsTab
          onDismiss={() => {
            actionTab = null;
          }}
        />
      {/if}
      <div class="divider" />
      <LeftPanelAccountBar bind:accountSettingsDialog />
    </div>

    <div class="panel right-panel">
      <slot />
    </div>
  </div>
</div>

<style lang="scss">
  div.viewport {
    width: 100vw;
    height: 100vh;

    display: flex;

    position: fixed;
    flex-direction: column;

    left: 0px;
    top: 0px;
  }

  div.panel-container {
    flex-grow: 1;

    width: 100%;

    display: flex;
    min-height: 0px;

      background-color: var(--primaryContainer);

    > div.panel {
      box-sizing: border-box;
    }

    > div.left-panel {
      min-width: 320px;
      max-width: 320px;
      height: 100%;

      display: flex;
      flex-direction: column;

      min-height: 0px;

      padding: 0px 16px 0px 16px;

      > div.divider {
        min-height: 1px;
        max-height: 1px;
        background-color: var(--primary);
      }

      > div.actions {
        display: flex;
        gap: 8px;

        > button {
          cursor: pointer;

          flex-grow: 1;

          display: flex;
          gap: 8px;
          align-items: center;
          justify-content: center;

          color: var(--primary);
          background-color: unset;
          border: unset;
        }
      }
    }

    > div.right-panel {
      height: 100%;

      flex-grow: 1;

      min-width: 0px;

      border-radius: 16px 0px 0px 0px;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>

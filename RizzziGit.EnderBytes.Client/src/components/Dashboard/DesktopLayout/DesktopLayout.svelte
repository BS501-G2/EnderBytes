<script lang="ts" context="module">
  enum ActionTab {
    Operations,
    Notification,
  }
</script>

<script lang="ts">
  import type { Client } from "$lib/client/client";

  import { BellIcon, CheckSquareIcon } from "svelte-feather-icons";

  import LeftPanelNavigationBar from "./LeftPanelNavigationBar.svelte";
  import LeftPanelAccountBar from "./LeftPanelAccountBar.svelte";
  import TitleBar from "./TitleBar.svelte";
  import Keyboard from "../../Bindings/Keyboard.svelte";
  import OperationsTab from "./OperationsTab.svelte";
  import NotificationsTab from "./NotificationsTab.svelte";
  import BackgroundTaskList, {
    pendingTasks,
  } from "../../BackgroundTaskList/BackgroundTaskList.svelte";

  export let client: Client;

  let actionTab: ActionTab | null = null;
</script>

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
      <LeftPanelAccountBar {client} />
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

    > div.panel {
      box-sizing: border-box;
    }

    > div.left-panel {
      min-width: 256px;
      max-width: 256px;
      height: 100%;

      background-color: var(--primaryContainer);

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
    }
  }
</style>

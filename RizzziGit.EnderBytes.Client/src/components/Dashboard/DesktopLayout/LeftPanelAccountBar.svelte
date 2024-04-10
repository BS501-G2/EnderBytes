<script lang="ts">
  import { goto } from "$app/navigation";
  import { RootState } from "$lib/states/root-state";

  import { UserIcon, LogOutIcon, SettingsIcon } from "svelte-feather-icons";
  import Dialog, { DialogClass } from "../../../components/Dialog.svelte";
  import { ButtonClass } from "../../../components/Button.svelte";
  import type { Client } from "$lib/client/client";

  export let client: Client;

  const rootState = RootState.state;

  let logoutConfirm: boolean = false;
</script>

{#if logoutConfirm}
  <Dialog
    dialogClass={DialogClass.Normal}
    buttons={[
      {
        label: "OK",
        buttonClass: ButtonClass.Primary,
        onClick: () => client.logout(),
      },
      {
        label: "Cancel",
        buttonClass: ButtonClass.Background,
        onClick: () => {
          logoutConfirm = false;
        },
      },
    ]}
    title="Account Logout"
    onDismiss={() => (logoutConfirm = false)}
  >
    <span slot="body">This will log you out from the dashboard.</span>
  </Dialog>
{/if}

<div class="account">
  <button
    class="account-info"
    on:click={() => goto(`/app/profile/:${client.session?.userId ?? "null"}`)}
  >
    <UserIcon />
    <p>
      {#if client.session != null}
        {#await client.getLoginUser()}
          ...
        {:then user}
          {user.LastName}, {user.FirstName[0]}.{user.MiddleName[0]
            ? `${user.MiddleName[0]}.`
            : ""}
        {/await}
      {/if}
    </p>
  </button>
  <div class="account-actions">
    <button on:click={() => (logoutConfirm = true)}><LogOutIcon /></button>
    <button on:click={() => goto("/app/settings")}><SettingsIcon /></button>
  </div>
</div>

<style lang="scss">
  div.account {
    display: flex;
    flex-direction: row;

    // padding: 8px;

    > div.account-actions {
      display: flex;
      flex-direction: row;

      > button {
        cursor: pointer;

        background-color: transparent;
        color: var(--primary);

        border-style: solid;
        border-width: 1px;
        border-color: transparent;

        display: flex;
        flex-direction: row;
        align-items: center;
      }

      > button:hover {
        border-color: var(--primary);
      }

      > button:active {
        background-color: var(--primary);
        color: var(--onPrimary);
      }
    }

    > button.account-info {
      width: 100%;

      display: flex;
      flex-direction: row;
      align-items: center;
      padding: 8px;
      gap: 8px;

      cursor: pointer;
      user-select: none;

      background-color: inherit;
      color: inherit;

      border-style: solid;
      border-width: 1px;
      border-color: transparent;
      min-width: 0px;

      > p {
        margin: 0px;

        overflow-x: hidden;
        text-overflow: ellipsis;
        text-wrap: nowrap;
      }
    }

    > button.account-info:hover {
      border-color: var(--primary);
    }

    > button.account-info:active {
      background-color: var(--primary);
      color: var(--onPrimary);
    }
  }
</style>

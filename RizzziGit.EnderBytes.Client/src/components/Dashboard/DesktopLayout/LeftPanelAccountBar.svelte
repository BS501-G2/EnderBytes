<script lang="ts">
  import { goto } from "$app/navigation";
  import { RootState } from "$lib/states/root-state";

  import { UserIcon, LogOutIcon, SettingsIcon } from "svelte-feather-icons";
  import Dialog, { DialogClass } from "../../../components/Dialog.svelte";
  import { ButtonClass } from "../../../components/Button.svelte";

  const rootState = RootState.state;

  let logoutConfirm: boolean = false;
</script>

{#await $rootState.getClient() then client}
  {#if logoutConfirm}
    <Dialog
      dialogClass={DialogClass.Normal}
      buttons={[
        {
          label: "OK",
          buttonClass: ButtonClass.PrimaryContainer,
          onClick: () => client.logout(),
        },
        {
          label: "Cancel",
          buttonClass: ButtonClass.PrimaryContainer,
          onClick: () => {
            logoutConfirm = false;
          },
        },
      ]}
      onDismiss={() => (logoutConfirm = false)}
    >
      <h2 slot="header" style="margin: 0px;">Account Logout</h2>
      <p slot="body">This will log you out from the dashboard.</p>
    </Dialog>
  {/if}

  <div class="account">
    <button
      class="account-info"
      on:click={() => goto(`/app/profile/:${client.session?.userId ?? "null"}`)}
    >
      <UserIcon />
      <p>
        {#await client.getLoginUser()}
          ...
        {:then user}
          {user.LastName}, {user.FirstName[0]}.{user.MiddleName[0]
            ? `${user.MiddleName[0]}.`
            : ""}
        {/await}
      </p>
    </button>
    <div class="account-actions">
      <button on:click={() => (logoutConfirm = true)}><LogOutIcon /></button>
      <button on:click={() => goto("/app/settings")}><SettingsIcon /></button>
    </div>
  </div>
{/await}

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

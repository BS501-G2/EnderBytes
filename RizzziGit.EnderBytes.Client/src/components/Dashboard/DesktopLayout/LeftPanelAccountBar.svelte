<script lang="ts">
  import { goto } from "$app/navigation";

  import { UserIcon, LogOutIcon, SettingsIcon } from "svelte-feather-icons";
  import Dialog, { DialogClass } from "../../Widgets/Dialog.svelte";
  import Button, { ButtonClass } from "../../Widgets/Button.svelte";
  import Client, { interpretResponse } from "../../Bindings/Client.svelte";

  export let accountSettingsDialog: boolean;

  let logoutConfirm: boolean = false;
</script>

{#if logoutConfirm}
  <Dialog
    dialogClass={DialogClass.Normal}
    onDismiss={() => (logoutConfirm = false)}
  >
    <svelte:fragment slot="actions">
      <Client let:fetch>
        <Button
          onClick={() => fetch("/auth/logout", "POST")}
          buttonClass={ButtonClass.Primary}
        >
          OK
        </Button>
        <Button
          onClick={() => {
            logoutConfirm = false;
          }}
          buttonClass={ButtonClass.Background}
        >
          Cancel
        </Button>
      </Client>
    </svelte:fragment>
    <h2 slot="head" style="margin: 0px;">Account Logout</h2>
    <span slot="body">This will log you out from the dashboard.</span>
  </Dialog>
{/if}

<div class="account">
  <Client let:session let:fetchAndInterpret>
    <button
      class="account-info"
      on:click={() => goto(`/app/profile/:${session?.userId ?? "null"}`)}
    >
      <UserIcon />
      <p>
        {#if session != null}
          {#await fetchAndInterpret("/user/!me", "GET")}
            ...
          {:then user}
            {user.lastName}, {user.firstName[0]}.{user.middleName[0]
              ? `${user.middleName[0]}.`
              : ""}
          {/await}
        {/if}
      </p>
    </button>
  </Client>
  <div class="account-actions">
    <button on:click={() => (logoutConfirm = true)}><LogOutIcon /></button>
    <button on:click={() => (accountSettingsDialog = true)}>
      <SettingsIcon />
    </button>
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

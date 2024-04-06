<script lang="ts">
  import { UserIcon, LogOutIcon, SettingsIcon } from "svelte-feather-icons";
</script>

<div class="account">
  <button class="account-info">
    <UserIcon />
    <p>asd</p>
  </button>
  <div class="account-actions">
    <button><LogOutIcon /></button>
    <button><SettingsIcon /></button>
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

      > p {
        margin: 0px;
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

<script lang="ts">
  import { BellIcon } from "svelte-feather-icons";
</script>

<div class="action-chip">
  <button>
    <span>1</span>
    <BellIcon size="16" />
  </button>
</div>

<style lang="scss">
  div.action-chip {
    background-color: var(--primary);
    color: var(--onPrimary);

    margin: 8px;
    border-radius: 8px;

    display: flex;
    justify-content: center;

    padding: 0px 8px 0px 8px;

    > button {
      cursor: pointer;
      background-color: var(--primary);
      color: var(--onPrimary);

      border: solid 1px transparent;

      display: flex;
      align-items: center;
    }

    > button:hover {
      border-color: var(--onPrimary);
    }

    > button:active {
      background-color: var(--onPrimary);
      color: var(--primary);
    }
  }
</style>

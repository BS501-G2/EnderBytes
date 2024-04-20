<script lang="ts" context="module">
  export type OnClickFunction = () => any | Promise<any>;
</script>

<script lang="ts">
  export let onClick: OnClickFunction;
</script>

<button on:click={onClick}>
  <slot />
</button>

<style lang="scss">
  button {
    display: flex;
    align-items: center;

    max-height: 100%;
    min-height: 100%;

    cursor: pointer;

    border-radius: 8px;

    background-color: transparent;
    color: var(--onPrimary);

    outline: none;
    border: none;
  }

  button:hover {
    background-color: #ffffff7f;
  }

  button:active {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);
  }
</style>

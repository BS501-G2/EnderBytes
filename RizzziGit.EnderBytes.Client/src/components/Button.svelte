<script lang="ts" context="module">
  export enum ButtonClass {
    Primary = "primary",
    PrimaryContainer = "primary-container",
    Background = "background",
  }
</script>

<script lang="ts">
  export let buttonClass: ButtonClass;
  export let enabled: boolean = true;
  export let onClick: () => Promise<void> | void;
  export let label: string;

  let busy: boolean = false;
</script>

<button
  disabled={!enabled || busy}
  class="button {buttonClass}"
  on:click={onClick}><p>{label}</p></button
>

<style lang="scss">
  button.button {
    width: 100%;
    padding: 8px 16px 8px 16px;

    box-sizing: border-box;

    border: solid 1px transparent;
    border-radius: 8px;

    transition: all linear 150ms;

    cursor: pointer;

    > p {
      margin: 0px;
    }
  }

  button.primary {
    background-color: var(--primary);
    color: var(--onPrimary);
  }

  button.primary:hover {
    background-color: var(--primaryVariant);
    color: var(--onPrimaryVariant);
  }

  button.primary:active {
    background-color: var(--onPrimary);
    color: var(--primary);
  }

  button.primary-container {
    background-color: var(--primaryContainer);
    color: var(--onPrimaryContainer);
  }

  button.primary-container:hover {
    background-color: var(--primaryContainerVariant);
    color: var(--onPrimaryContainer);
  }

  button.primary-container:active {
    background-color: var(--onPrimaryContainer);
    color: var(--primaryContainer);
  }

  button.background {
    background-color: var(--background);
    color: var(--onBackground);
  }

  button.background:hover {
    background-color: var(--backgroundVariant);
    color: var(--onBackground);
  }

  button.background:active {
    background-color: var(--onBackground);
    color: var(--background);
  }
</style>

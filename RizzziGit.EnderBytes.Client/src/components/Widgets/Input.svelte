<script context="module" lang="ts">
  export enum InputClass {
    Primary = "primary",
    PrimaryContainer = "primary-container",
    Background = "background",
  }
</script>

<script lang="ts">
  import type { ChangeEventHandler } from "svelte/elements";

  export let inputClass: InputClass = InputClass.Background;
  export let type: HTMLInputElement["type"] = "text";
  export let name: string;
  export let required: boolean = false;
  export let disabled: boolean = false;
  export let text: string = "";
  export let placeholder: string = "Type something...";
  export let valid: boolean = true;
  export let validate: (() => boolean) | null = null;

  let element: HTMLInputElement | null = null;

  const onChange: ChangeEventHandler<HTMLInputElement> = (event) => {
    text = event.currentTarget.value || "";
    valid = validate?.() ?? element?.checkValidity() ?? true;
  };
</script>

<div class="input {inputClass}">
  <button on:click={() => element?.focus()}><p>{name}</p></button>
  <input
    bind:this={element}
    {type}
    {name}
    {required}
    {disabled}
    {placeholder}
    on:change={onChange}
  />
</div>

<style lang="scss">
  div.input {
    color: var(--onPrimaryContainer);

    display: flex;
    flex-direction: column;

    cursor: text;

    padding: 4px 8px 4px 8px;

    border-radius: 8px;

    > button {
      background-color: unset;
      color: unset;
      border: unset;

      padding: 0px;

      font-size: 12px;

      cursor: unset;

      > p {
        margin: 0px;

        text-align: left;
      }
    }

    > input {
      background-color: var(--background);
      color: unset;
      border: unset;

      outline: none;

      font-size: 14px;
      border-radius: 8px;
    }
  }

  div.input.primary {
    border: solid 1px var(--onPrimary);

    > button {
      color: var(--onPrimary);
    }

    > input {
      background-color: var(--primary);
      color: var(--onPrimary);
    }
  }

  div.input.primary-container {
    border: solid 1px var(--primaryContainer);

    > button {
      color: var(--primaryContainer);
    }

    > input {
      background-color: var(--primaryContainer);
      color: var(--onPrimaryContainer);
    }
  }

  div.input.background {
    border: solid 1px var(--onBackground);

    > button {
      color: var(--onBackground);
    }

    > input {
      background-color: var(--background);
      color: var(--onBackground);
    }
  }

  div.input:focus-within {
    outline: -webkit-focus-ring-color auto 1px;
  }
</style>

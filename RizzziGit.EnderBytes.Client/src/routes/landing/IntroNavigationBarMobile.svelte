<script lang="ts" context="module">
  import { introNavigationButtons } from '$lib/intro-navigation';
</script>

<script lang="ts">
  import { goto } from '$app/navigation';
  let scroll: number = 0;
</script>

<svelte:window bind:scrollY={scroll} />
<svelte:body />

<div class="navigation-bar">
  <div>
    <button on:click={() => goto(introNavigationButtons[0].path)}
      >{introNavigationButtons[0].name}</button
    >
  </div>
</div>

<style lang="scss">
  div.navigation-bar {
    position: fixed;
    left: 0px;
    bottom: 0px;

    display: flex;
    width: 100%;

    background-color: var(--primaryContainer);

    padding: 8px;
    box-sizing: border-box;

    border-radius: 8px 8px 0px 0px;

    > div {
      width: 100%;
      box-sizing: border-box;

      > button {
        background-color: var(--primary);
        color: var(--onPrimary);

        box-sizing: border-box;

        width: 100%;
        padding: 8px;

        border-style: solid;
        border-color: #00000000;
        border-radius: 8px;

        font-size: 14px;

        transition: cubic-bezier(0.075, 0.82, 0.165, 1);
        transition-duration: 200ms;

        cursor: pointer;
      }

      > button:nth-child(2) {
        background-color: transparent;
      }

      > button:hover {
        border-color: var(--onPrimary);
      }
    }
  }
</style>

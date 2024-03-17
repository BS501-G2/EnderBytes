<script lang=ts>
  import { ClockIcon, FlagIcon } from "svelte-feather-icons";

  const navigation = [
    ['My Files', '/app/files', ClockIcon],
    ['Shared', '/app/shared', FlagIcon],
    ['Feed', '/app/feed', ClockIcon]
  ]
</script>

<div class="side-bar">
  {#each navigation as entry}
    <svelte:element this={entry[2]} />
  {/each}
</div>

<style lang="scss">
  div.side-bar {
    background-color: var(--sidebar-background-color-01);

    position: fixed;
    left: 16px;
    top: 16px;

    box-sizing: border-box;
    width: 64px;
    height: calc(100% - 32px);

    border-radius: 8px;

    box-shadow: var(--shadow-color-01) 2px 2px 8px;

    @media only screen and (max-width: 720px) {
      left: 0px;
      top: unset;
      bottom: 0px;

      width: 100%;
      height: 64px;

      border-radius: 8px 8px 0px 0px;

      border-width: 0px;

      box-shadow: var(--shadow-color-01) 0px -2px 8px;
    }
  }
</style>

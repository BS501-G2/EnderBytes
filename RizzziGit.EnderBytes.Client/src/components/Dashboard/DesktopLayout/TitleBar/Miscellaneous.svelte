<script lang="ts">
  import { BellIcon } from "svelte-feather-icons";
  import TitleBarChip from "./TitleBarChip.svelte";
  import TitleBarChipButton from "./TitleBarChip/Button.svelte";
  import OperationButton from "./Miscellaneous/OperationButton.svelte";

  const iconSize = "18em";

  let menuLocationAnchor: HTMLElement;
  let menuX: number = 0;
  let menuY: number = 0;

  function updateMenuLocation() {
    menuX = (window.innerWidth - menuLocationAnchor.offsetLeft) - menuLocationAnchor.clientWidth ;
    menuY = menuLocationAnchor.offsetTop + menuLocationAnchor.clientHeight + 8;
  }
</script>

<svelte:window on:resize={updateMenuLocation}/>

<TitleBarChip>
  <div class="actions" bind:this={menuLocationAnchor}>
    <TitleBarChipButton onClick={() => {}}>
      <BellIcon size={iconSize} />
    </TitleBarChipButton>
    <OperationButton {menuX} {menuY} {iconSize} {updateMenuLocation} />
  </div>
</TitleBarChip>

<style lang="scss">
  div.actions {
    display: flex;

    padding: 0px 8px 0px 8px;

    gap: 8px;
  }
</style>

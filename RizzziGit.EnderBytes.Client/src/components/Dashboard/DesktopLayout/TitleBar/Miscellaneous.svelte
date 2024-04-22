<script lang="ts">
  import { BellIcon } from "svelte-feather-icons";
  import TitleBarChip from "./TitleBarChip.svelte";
  import TitleBarChipButton from "./TitleBarChip/Button.svelte";
  import OperationButton from "./Miscellaneous/OperationButton.svelte";

  const iconSize = "18em";
</script>

<TitleBarChip>
  <div class="actions">
    <TitleBarChipButton onClick={() => {}}>
      <BellIcon size={iconSize} />
    </TitleBarChipButton>
    <OperationButton {iconSize} />
  </div>
</TitleBarChip>

<style lang="scss">
  div.actions {
    display: flex;

    padding: 8px;

    gap: 8px;
  }
</style>

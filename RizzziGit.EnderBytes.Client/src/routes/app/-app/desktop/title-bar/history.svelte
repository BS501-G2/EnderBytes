<script lang="ts">
  import { ChevronLeftIcon, ChevronRightIcon } from 'svelte-feather-icons';

  import TitleBarChip from './chip.svelte';
  import TitleBarChipButton from './chip/button.svelte';
</script>

<TitleBarChip>
  <TitleBarChipButton onClick={() => history.back()}>
    <ChevronLeftIcon strokeWidth={2} size="18em" />
  </TitleBarChipButton>
</TitleBarChip>

<TitleBarChip>
  <TitleBarChipButton onClick={() => history.forward()}>
    <ChevronRightIcon strokeWidth={2} size="18em" />
  </TitleBarChipButton>
</TitleBarChip>

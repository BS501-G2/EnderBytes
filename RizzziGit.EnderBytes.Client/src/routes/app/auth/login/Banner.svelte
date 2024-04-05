<script lang="ts">
  import { ColorKey } from "$lib/color-schemes";
  import { RootState } from "$lib/states/root-state";

  const rootState = RootState.state;

  function color(key: ColorKey) {
    return $rootState.getColor(key) >> 8;
  }
</script>

<!-- <rect width="32px" height="100%"> </rect> -->

<script lang="ts">
  // import { timedTransition } from "$lib/derived/state-transition";
  import { writable } from "svelte/store";

  type Position = [x: number, y: number];

  // output.set(current = target[0] + ((target[1] - target[0]) * progress))

  // if (progress >= 1) {
  //   target = null
  //   return
  // }

  let position = writable<Position>([0, 0]);

  let windowWidth: number;
  let windowHeight: number;

  // const animatedPosition = timedTransition(
  //   position,
  //   1000,
  //   ([fromX, fromY], [toX, toY], progress): Position => {
  //     return [
  //       fromX + (toX - fromX) * progress,
  //       fromY + (toY - fromY) * progress,
  //     ];
  //   },
  // );
</script>
<div style="width: 100px; height:100px; background-color: blue;" style:focus="backgrond-color:green;"></div>

<!-- https://www.benmvp.com/blog/how-to-create-circle-svg-gradient-loading-spinner -->

<script lang="ts">
  import { ColorKey } from "$lib/color-schemes";
  import { RootState } from "$lib/states/root-state";

  const rootState = RootState.state;
</script>

<svg
  viewBox="0 0 200 200"
  color={$rootState.getColorHex(ColorKey.Primary)}
  fill="none"
  xmlns="http://www.w3.org/2000/svg"
>
  <defs>
    <linearGradient id="spinner-secondHalf">
      <stop offset="0%" stop-opacity="0" stop-color="currentColor" />
      <stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
    </linearGradient>
    <linearGradient id="spinner-firstHalf">
      <stop offset="0%" stop-opacity="1" stop-color="currentColor" />
      <stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
    </linearGradient>
  </defs>

  <g stroke-width="8">
    <path stroke="url(#spinner-secondHalf)" d="M 4 100 A 96 96 0 0 1 196 100" />
    <path stroke="url(#spinner-firstHalf)" d="M 196 100 A 96 96 0 0 1 4 100" />

    <path
      stroke="currentColor"
      stroke-linecap="round"
      d="M 4 100 A 96 96 0 0 1 4 98"
    />
  </g>

  <animateTransform
    from="0 0 0"
    to="360 0 0"
    attributeName="transform"
    type="rotate"
    repeatCount="indefinite"
    dur="1300ms"
  />
</svg>

<style lang="scss">
  svg {
    width: 100%;
    height: 100%;
  }
</style>

<!-- https://www.benmvp.com/blog/how-to-create-circle-svg-gradient-loading-spinner -->

<script lang="ts" context="module">
  import { writable, type Writable } from "svelte/store";
  let spinner: Writable<number> = writable(0);
  let activeCount: number = 0;

  spinner.subscribe(console.log);

  function connect() {
    if (activeCount == 0) {
      const update = () => {
        if (activeCount <= 0) {
          activeCount = 0;

          return;
        }

        spinner.update((v) => {
          v += 6;

          if (v >= 360) {
            v = 0;
          }

          return v;
        });

        requestAnimationFrame(update);
      };

      activeCount++;
      requestAnimationFrame(update);
      return;
    } else {
      activeCount++;
    }
  }

  function disconnect() {
    activeCount--;
  }
</script>

<script lang="ts">
  import { onDestroy, onMount } from "svelte";

  onMount(() => {
    connect();
  });

  onDestroy(() => disconnect());
</script>

<svg
  viewBox="0 0 200 200"
  fill="none"
  style="transform: rotate({$spinner}deg);"
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

  <!-- <animateTransform
    from="0 0 0"
    to="360 0 0"
    attributeName="transform"
    type="rotate"
    repeatCount="indefinite"
    dur="1300ms"
  /> -->
</svg>

<style lang="scss">
  svg {
    width: 100%;
    height: 100%;
  }
</style>

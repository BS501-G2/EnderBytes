<script lang="ts">
  import AnimationFrame from "../../components/Bindings/AnimationFrame.svelte";

  let a: number = 0;

  function onFrame() {
    a += 1;
  }
</script>

<AnimationFrame callback={onFrame} />

<div class="container">
  <div
    class="cube"
    style="transform: rotateX({a}deg) rotateY({Math.sin(a * 0.1) *10 }deg) translateZ(90px);"
  />
</div>

<style lang="scss">
  div.container {
    width: 100vw;
    height: 100vh;

    display: flex;
    align-items: center;
    justify-content: center;

    filter: drop-shadow(rgba(0, 0, 0, 0.5)  8px 8px 8px);

    > div.cube {
      width: 100px;
      height: 100px;

      background-color: var(--primary);
    }
  }
</style>

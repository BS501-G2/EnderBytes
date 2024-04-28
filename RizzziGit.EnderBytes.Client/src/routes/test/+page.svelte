<script lang="ts">
  import AnimationFrame from "../../components/Bindings/AnimationFrame.svelte";
  import ResponsiveLayout from "../../components/Bindings/ResponsiveLayout.svelte";
  import Button from "../../components/Widgets/Button.svelte";
  import Expandable from "../../components/Widgets/Expandable.svelte";

  let a: number = 0;

  function onFrame() {
    a += 1;
  }
</script>

<Expandable>
  <svelte:fragment slot="header" let:toggle>
    <Button onClick={toggle}>Click me</Button>
  </svelte:fragment>
  <svelte:fragment slot="body">
    <p>Expand me</p>
  </svelte:fragment>
</Expandable>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <h1>This is desktop.</h1>
  </svelte:fragment>
  <svelte:fragment slot="mobile">
    <h1>This is mobile.</h1>
  </svelte:fragment>
</ResponsiveLayout>

<AnimationFrame callback={onFrame} />

<div class="container">
  <div
    class="cube"
    style="transform: rotateX({a}deg) rotateY({Math.sin(a * 0.1) *
      10}deg) translateZ(90px);"
  />
</div>

<style lang="scss">
  div.container {
    width: 100vw;
    height: 100vh;

    display: flex;
    align-items: center;
    justify-content: center;

    filter: drop-shadow(rgba(0, 0, 0, 0.5) 8px 8px 8px);

    > div.cube {
      width: 100px;
      height: 100px;

      background-color: var(--primary);
    }
  }
</style>

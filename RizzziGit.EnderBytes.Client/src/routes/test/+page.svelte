<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import Awaiter from "../../components/Bindings/Awaiter.svelte";
  import LoadingBar from "../../components/Widgets/LoadingBar.svelte";
  import Input from "../../components/Widgets/Input.svelte";

  export let progress: number | null = 0;

  let a: boolean = false;

  async function exec() {
    while (a) {
      if (progress != null) {
        progress += 0.001;

        if (progress >= 1) {
          progress = null;
        }
        await new Promise((resolve) => setTimeout(resolve, 30 / 1000));
      } else {
        await new Promise((resolve) => setTimeout(resolve, 1000));
        progress = 0;
      }
    }
  }

  onMount(() => {
    a = true;
    exec();
  });

  onDestroy(() => {
    a = false;
  });

  let text: string;
  let valid: boolean
</script>

<div class="content">
  <!-- <LoadingBar {progress} /> -->
  <Input name="test" type="email" bind:valid bind:text />
  <p>{text}</p>
  <p>Valid? {valid}</p>
  <Awaiter
    callback={async () => {
      await new Promise((resolve) => setTimeout(resolve, 1000));

      throw new Error("Test");
    }}
  ></Awaiter>
</div>

<style lang="scss">
  div.content {
    width: 100%;
    height: 100%;

    position: fixed;
    top: 0;
    left: 0;
  }
</style>

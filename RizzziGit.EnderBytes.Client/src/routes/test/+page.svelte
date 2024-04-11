<script lang="ts">
  import Awaiter from "../../components/Awaiter.svelte";
  import LoadingBar from "../../components/LoadingBar.svelte";
</script>

<div class="content">
  <LoadingBar />
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

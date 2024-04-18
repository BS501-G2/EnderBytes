<script lang="ts">
  import AddressBar from "./DesktopLayout/AddressBar.svelte";
  import ControlBar from "./DesktopLayout/ControlBar.svelte";
  import FileArea from "./DesktopLayout/FileArea.svelte";

  export let currentFileId: number | null;

  let selectedFiles: any[];
  let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;

  $: {
    currentFileId;

    selectedFiles = [];
  }
</script>

<div class="content">
  {#key currentFileId}
    <AddressBar bind:currentFileId />
    <div class="bezel">
      <ControlBar bind:currentFileId bind:selectedFiles bind:onRefresh />
      <FileArea bind:onRefresh bind:currentFileId bind:selectedFiles />
    </div>
  {/key}
</div>

<style lang="scss">
  div.content {
    background-color: var(--primaryContainer);
    color: var(--onPrimaryContainer);

    width: 100%;
    height: 100%;

    display: flex;
    flex-direction: column;

    > div.bezel {
      flex-grow: 1;

      display: flex;
      flex-direction: column;

      border-radius: 16px 0px 0px 0px;
      overflow: hidden;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>

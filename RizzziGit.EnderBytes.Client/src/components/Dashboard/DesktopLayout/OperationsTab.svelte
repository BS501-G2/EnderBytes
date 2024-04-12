<script lang="ts">
  import BackgroundTasks, {
    BackgroundTaskStatus,
    dismissAll,
    executeBackgroundTask,
  } from "../../BackgroundTaskList.svelte";
  import Button from "../../Button.svelte";
  import NavigationTab from "./NavigationTab.svelte";

  export let onDismiss: () => void;

  async function run(): Promise<void> {
    let progress: number | null = null;

    const backgroundTask = executeBackgroundTask(
      `TEST ${Date.now()}`,
      true,
      async (client, setStatus) => {
        await new Promise<void>((resolve) => setTimeout(resolve, 1000));
        progress = 0;

        while (progress <= 1) {
          setStatus("Downloading", progress);
          progress += 0.01;

          await new Promise<void>((resolve) =>
            setTimeout(resolve, 1000 / (Math.random() * 25 + 5)),
          );
        }

        // throw new Error("Test");
      },
    );

    await backgroundTask.run();
  }
</script>

<NavigationTab {onDismiss}>
  <svelte:fragment slot="head"><p><b>Pending Operations</b></p></svelte:fragment
  >
  <svelte:fragment slot="body">
    <div class="list">
      <BackgroundTasks filter={null} />
    </div>
    <div class="buttons">
      <Button onClick={run}>Add</Button>
      <Button onClick={dismissAll}>Clear All</Button>
    </div>
  </svelte:fragment>
</NavigationTab>

<style lang="scss">
  div.buttons {
    padding: 8px 0px 8px 0px;

    display: flex;
    flex-direction: column;
  }

  div.list {
    display: flex;

    flex-direction: column;

    flex-grow: 1;

    min-height: 0px;
    overflow-y: auto;
  }
</style>

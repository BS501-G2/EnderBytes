<script lang="ts">
  import { goto } from "$app/navigation";
  import type { Client } from "$lib/client/client";
  import { executeBackgroundTask } from "../BackgroundTaskList/BackgroundTaskList.svelte";
  import Awaiter from "../Bindings/Awaiter.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";
  import Dialog from "../Widgets/Dialog.svelte";
  import Input from "../Widgets/Input.svelte";
  import LoadingSpinner from "../Widgets/LoadingSpinner.svelte";

  export let client: Client;
  export let currentFileId: number | null;
  export let onCancel: () => void;

  async function createFolder(name: string): Promise<number> {
    const result = <number>await executeBackgroundTask(
      "Folder Creation",
      true,
      async (_, setStatus) => {
        const folderId = await client.createFolder(name, currentFileId);
        setStatus(`Folder "${name}" created.`);

        goto(`/app/files/${folderId}`);
      },
      false,
    ).run();
    onCancel();
    return result;
  }

  let name: string = "";
</script>

<Dialog onDismiss={onCancel}>
  <h2 style="margin: 0px" slot="head">Create new folder.</h2>
  <svelte:fragment slot="body">
    <p style="margin-top: 0px">
      New folder will be created inside the current folder.
    </p>
    <Input name="Folder name" bind:text={name} />
  </svelte:fragment>
  <svelte:fragment slot="actions">
    <div class="actions">
      <Awaiter callback={() => createFolder(name)} autoLoad={false}>
        <svelte:fragment slot="not-loaded" let:load>
          <Button onClick={load}>Create</Button>
          <Button buttonClass={ButtonClass.Background} onClick={onCancel}>
            Cancel
          </Button>
        </svelte:fragment>
        <svelte:fragment slot="error" let:error let:reset>
          <span class="error-message">
            {error.errorMessage}
          </span>
          <Button onClick={() => reset(false)}>Retry</Button>
        </svelte:fragment>

        <svelte:fragment slot="loading">
          <div class="loading">
            <LoadingSpinner></LoadingSpinner>
          </div>
        </svelte:fragment>
      </Awaiter>
    </div>
  </svelte:fragment>
</Dialog>

<style lang="scss">
  div.actions {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    > span.error-message {
      max-width: 256px;

      text-overflow: ellipsis;
      overflow-x: hidden;
      text-wrap: nowrap;

      color: var(--error);
    }

    > div.loading {
      display: flex;
      align-items: center;
      justify-content: center;

      max-height: 32px;
      max-width: 32px;
    }
  }
</style>

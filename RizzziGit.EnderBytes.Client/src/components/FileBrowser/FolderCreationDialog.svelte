<script lang="ts" context="module">
  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import { goto } from "$app/navigation";
  import { writable, type Writable } from "svelte/store";
  import { executeBackgroundTask } from "../BackgroundTaskList.svelte";
  import Awaiter from "../Bindings/Awaiter.svelte";
  import { apiFetch } from "../Bindings/Client.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";
  import Dialog from "../Widgets/Dialog.svelte";
  import Input from "../Widgets/Input.svelte";
  import LoadingSpinner from "../Widgets/LoadingSpinner.svelte";

  export let currentFileId: number | null;

  async function createFolder(name: string): Promise<number> {
    const result = <number>await executeBackgroundTask(
      "Folder Creation",
      true,
      async (_, setStatus) => {
        setStatus(`Creating folder "${name}"...`);
        const folder = await apiFetch(
          `/file/${currentFileId != null ? `:${currentFileId}` : "!root"}/files/new-folder`,
          "POST",
          {
            name: name,
            isFolder: true,
          },
        );
        setStatus(`Folder "${name}" created.`);

        goto(`/app/files/${folder.id}`);
      },
      false,
    ).run();
    $enabled = false;
    return result;
  }

  let name: string = "";

  let load: () => Promise<void>;
</script>

{#if $enabled}
  <Dialog onDismiss={() => ($enabled = false)}>
    <h2 slot="head">Create Folder</h2>
    <div class="body" slot="body">
      <p style="margin-top: 0px">
        New folder will be created inside the current folder.
      </p>
      <Input name="Folder name" bind:text={name} onSubmit={load} />
    </div>
    <svelte:fragment slot="actions">
      <div class="actions">
        <Awaiter callback={() => createFolder(name)} autoLoad={false} bind:load>
          <svelte:fragment slot="not-loaded">
            <Button onClick={load}>Create</Button>
            <Button
              buttonClass={ButtonClass.Background}
              onClick={() => ($enabled = false)}
            >
              Cancel
            </Button>
          </svelte:fragment>
          <svelte:fragment slot="error" let:error let:reset>
            <span class="error-message">
              {error.message}
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
{/if}

<style lang="scss">
  div.body {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

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

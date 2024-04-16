<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { executeBackgroundTask } from "../BackgroundTaskList/BackgroundTaskList.svelte";
  import Awaiter from "../Bindings/Awaiter.svelte";
  import ClientAwaiter from "../Bindings/ClientAwaiter.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";
  import Dialog from "../Widgets/Dialog.svelte";
  import Input from "../Widgets/Input.svelte";
  import LoadingSpinner from "../Widgets/LoadingSpinner.svelte";

  export let currentFileId: number | null;
  export let onCancel: () => void;

  let fileInput: HTMLInputElement;

  let files: FileList | null = null;

  let load: () => Promise<void>;
  let openDialog: () => void;

  async function createFile(client: Client): Promise<number> {
    if (files == null || files.length == 0) {
      throw new Error("No files selected");
    }

    const result = <number>await executeBackgroundTask(
      "File Upload",
      true,
      async (_, setStatus) => {
        onCancel();
        const delay = async () => {};
        // const delay = () => new Promise((resolve) => setTimeout(resolve, 100));

        setStatus("Creating file resource...", null);

        await delay();
        for (const file of files ?? []) {
          const bufferSize = 32 * 1024;

          let promises: Promise<any>[] = [];

          for (
            let fileOffset = 0;
            fileOffset < file.size;
            fileOffset += bufferSize
          ) {
            const capturedFileOffset = fileOffset;
            const sliced = await file
              .slice(capturedFileOffset, capturedFileOffset + bufferSize)
              .arrayBuffer();

            promises.push(
              (async () => {
                await client.sendToVoid(sliced);

                setStatus(
                  file.name,
                  capturedFileOffset / file.size,
                );

                if (_.cancelled) {
                  throw new Error("Cancelled");
                }
              })(),
            );
          }

          await Promise.all(promises);

          setStatus("Uploading content for " + file.name + " completed");
          await delay();
        }
        return 0;
      },
      false,
    ).run();

    return result;
  }
</script>

<input type="file" bind:files multiple hidden bind:this={fileInput} />

<ClientAwaiter let:client>
  <Dialog onDismiss={onCancel}>
    <h2 slot="head">Upload</h2>
    <div class="body" slot="body">
      <p style="margin-top: 0px">
        The uploaded files will be put inside the current folder. Alternatively,
        you can drag and drop files on the folder's area.
      </p>

      <!-- <Input name="File name" bind:text={name} onSubmit={load} /> -->
    </div>
    <svelte:fragment slot="actions">
      <Awaiter callback={() => createFile(client)} autoLoad={false} bind:load>
        <svelte:fragment slot="not-loaded">
          <div class="button">
            <Button
              onClick={() => {
                fileInput.click();
              }}
              buttonClass={ButtonClass.Background}
            >
              <p>
                {#if files?.length === 1}
                  {files[0].name}
                {:else if files?.length ?? 0 > 1}
                  {files?.length} Files
                {:else}
                  Cilck here to select files.
                {/if}
              </p>
            </Button>
          </div>

          <div class="button">
            <Button onClick={load}>Upload</Button>
          </div>
          <div class="button">
            <Button onClick={onCancel}>Cancel</Button>
          </div>
        </svelte:fragment>
      </Awaiter>
    </svelte:fragment>
    <!-- <div class="actions" slot="actions">
      <Awaiter
        callback={() => createFile(client, name)}
        autoLoad={false}
        bind:load
      >
        <svelte:fragment slot="not-loaded">
          <Button onClick={load}>Create</Button>
          <Button buttonClass={ButtonClass.Background} onClick={onCancel}>
            Cancel
          </Button>
        </svelte:fragment>
        <svelte:fragment slot="error" let:error let:reset>
          <span class="error-message">
            Error: {error.errorMessage ?? error.message}
          </span>
          <Button onClick={() => reset(false)}>Retry</Button>
        </svelte:fragment>

        <svelte:fragment slot="loading">
          <div class="loading">
            <LoadingSpinner></LoadingSpinner>
          </div>
        </svelte:fragment>
      </Awaiter>
    </div> -->
  </Dialog>
</ClientAwaiter>

<style lang="scss">
  div.body {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  div.button {
    display: flex;
    flex-direction: column;
  }

  div.button:nth-child(1) {
    flex-grow: 100;
  }
  div.button:nth-child(2) {
    flex-grow: 1;
  }
</style>

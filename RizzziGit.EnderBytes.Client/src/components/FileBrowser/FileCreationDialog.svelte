<script lang="ts" context="module">
  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import { executeBackgroundTask } from "../BackgroundTaskList.svelte";
  import Awaiter from "../Bindings/Awaiter.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";
  import Dialog from "../Widgets/Dialog.svelte";
  import { fetchAndInterpret } from "../Bindings/Client.svelte";
  import { writable, type Writable } from "svelte/store";

  export let currentFileId: number | null;

  let fileInput: HTMLInputElement;

  let files: FileList | null = null;

  let load: () => Promise<void>;

  async function createFile(): Promise<any[]> {
    if (files == null || files.length == 0) {
      throw new Error("No files selected");
    }

    // const client = await executeBackgroundTaskSeries<any[]>('File Upload', true, Array.from((files ?? [])).map<BackgroundTaskCallback<any>>((file) => async (client, setStatus) => {
    //   const bufferSize = 1024 * 1024
    //   for (let offset = 0; offset < file.size; offset += bufferSize) {
    //     const chunk = file.slice(offset, offset + bufferSize);

    //     setStatus('Uploading...', offset / file.size);
    //   }
    // }), false)

    const client = executeBackgroundTask<any[]>(
      "File Upload",
      true,
      async (client, setStatus) => {
        setStatus("Getting folder info...", null);

        for (const data of files ?? []) {
          const formData = new FormData();
          formData.append("offset", "0");
          formData.append("content", data, "content");

          const file = await fetchAndInterpret(
            `/file/${currentFileId != null ? `:${currentFileId}/files` : "!root"}`,
            "POST",
            { isFile: true, name: data.name },
          );
        }

        return [""];
      },
      false,
    );

    $enabled = false;
    return await client.run();
  }
</script>

<input type="file" bind:files multiple hidden bind:this={fileInput} />

{#if $enabled}
  <Dialog onDismiss={() => ($enabled = false)}>
    <h2 slot="head">Upload</h2>
    <div class="body" slot="body">
      <p style="margin-top: 0px">
        The uploaded files will be put inside the current folder. Alternatively,
        you can drag and drop files on the folder's area.
      </p>

      <!-- <Input name="File name" bind:text={name} onSubmit={load} /> -->
    </div>
    <svelte:fragment slot="actions">
      <Awaiter callback={() => createFile()} autoLoad={false} bind:load>
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
            <Button onClick={() => ($enabled = false)}>Cancel</Button>
          </div>
        </svelte:fragment>
      </Awaiter>
    </svelte:fragment>
  </Dialog>
{/if}

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

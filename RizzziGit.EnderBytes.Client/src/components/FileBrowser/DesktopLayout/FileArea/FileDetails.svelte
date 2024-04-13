<script lang="ts">
  import { FileIcon } from "svelte-feather-icons";
  import Awaiter from "../../../Bindings/Awaiter.svelte";
  import type { Client } from "$lib/client/client";

  export let client: Client;
  export let selectedFileIds: number[];
</script>

<div class="file-details">
  {#if selectedFileIds.length > 1}
    <p>{selectedFileIds.length} files</p>
  {:else if selectedFileIds.length === 1}
    {@const fileId = selectedFileIds[0]}
    {#key fileId}
      <Awaiter callback={() => client.getFile(fileId)}>
        <svelte:fragment slot="success" let:result={file}>
          <div class="file-preview">
            <img alt="File preview for `file`" src="/favicon.svg" />
          </div>
          <div class="file-info">
            <h2>{file.Name}</h2>
            <table>
              <tbody>
                <tr>
                  <td><p><b>Created On: </b></p></td>
                  <td><p>{new Date(file.CreateTime).toLocaleString()}</p></td>
                </tr>
                {#if file.UpdateTime !== file.CreateTime}
                  <tr>
                    <td><p><b>Modified On: </b></p></td>
                    <td><p>{new Date(file.UpdateTime).toLocaleString()}</p></td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </svelte:fragment>
      </Awaiter>
    {/key}
  {/if}
</div>

<style lang="scss">
  div.file-details {
    // background-color: var(--backgroundVariant);
    // color: var(--onBackgroundVariant);

    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-width: 320px;
    max-width: 320px;

    padding: 16px;

    overflow-y: auto;
    overflow-x: hidden;

    align-items: center;

    > div.file-preview {
      display: flex;
      flex-direction: column;

      width: 256px;
      height: 256px;
      box-sizing: border-box;

      align-items: center;

      padding: 16px;

      > img {
        width: 100%;
        height: 100%;
      }
    }

    > div.file-info {
      display: flex;
      flex-direction: column;

      gap: 16px;

      > h2 {
        margin: 0px;
        text-align: center;
      }
      > table {
        > tbody {
          > tr {
            > td {
              > p {
                margin: 0px;
              }
            }
          }
        }
      }
    }
  }
</style>

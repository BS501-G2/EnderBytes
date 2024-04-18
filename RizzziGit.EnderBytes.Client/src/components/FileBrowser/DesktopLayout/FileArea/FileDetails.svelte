<script lang="ts">
  export let selectedFiles: any[];
</script>

<div class="file-details">
  {#if selectedFiles.length > 1}
    <p>{selectedFiles.length} files</p>
  {:else if selectedFiles.length === 1}
    {@const file = selectedFiles[0]}
    {#key file}
      <div class="file-preview">
        <img alt={`File preview for \"${file.name}\"`} src="/favicon.svg" />
      </div>
      <div class="file-info">
        <h2>{file.name}</h2>
        <table>
          <tbody>
            <tr>
              <td><p><b>Created On: </b></p></td>
              <td>
                <p>{new Date(file.createTime).toLocaleString()}</p>
              </td>
            </tr>
            {#if file.UpdateTime !== file.createTime}
              <tr>
                <td><p><b>Modified On: </b></p></td>
                <td>
                  <p>{new Date(file.updateTime).toLocaleString()}</p>
                </td>
              </tr>
            {/if}
          </tbody>
        </table>
      </div>
    {/key}
  {/if}
</div>

<style lang="scss">
  div.file-details {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    border-radius: 16px;

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
              padding: 4px;

              > p {
                margin: 0px;
                text-align: right;
              }
            }
          }
        }
      }
    }
  }
</style>

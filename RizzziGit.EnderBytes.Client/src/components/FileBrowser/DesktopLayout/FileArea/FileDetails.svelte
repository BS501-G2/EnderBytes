<script lang="ts">
  import { FileIcon } from "svelte-feather-icons";
import Awaiter from "../../../Bindings/Awaiter.svelte";
  import { apiFetch } from "../../../Bindings/Client.svelte";
  import type { FileBrowserInformation, FileBrowserSelection } from "../../../FileBrowser.svelte";
  import LoadingSpinner from "../../../Widgets/LoadingSpinner.svelte";
  import UserFullName from "../../../Widgets/UserFullName.svelte";

  export let selection: FileBrowserSelection;
  export let info: FileBrowserInformation | null
</script>

<div class="file-details">
  {#if $selection.length > 1}
    <p>{$selection.length} files</p>
  {:else if $selection.length === 1}
    {@const file = $selection[0]}
    {#key file}
      <div class="file-preview">
        <img alt={`File preview for \"${file.name}\"`} src="/favicon.svg" />
      </div>
      <div class="file-info">
        <h2>{file.name}</h2>
        <table>
          <tbody>
            <tr>
              <td class="name"><p><b>Created On: </b></p></td>
              <td class="value">
                <p>{new Date(file.createTime).toLocaleString()}</p>
              </td>
            </tr>
            {#if file.UpdateTime !== file.createTime}
              <tr>
                <td class="name"><p><b>Modified On: </b></p></td>
                <td class="value">
                  <p>{new Date(file.updateTime).toLocaleString()}</p>
                </td>
              </tr>
            {/if}
            <tr>
              <td class="name"><p><b>Created By: </b></p></td>
              <td class="value">
                <p>
                  <Awaiter
                    callback={() =>
                      apiFetch(`/user/:${file.authorUserId}`)}
                  >
                    <svelte:fragment slot="success" let:result={user}>
                      <a class="user-link" href="/app/users/@{user.username}">
                        <UserFullName {user} />
                      </a>
                    </svelte:fragment>
                    <svelte:fragment slot="loading"
                      ><LoadingSpinner size="12px" /></svelte:fragment
                    >
                  </Awaiter>
                </p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    {/key}
  {:else if info != null}
    <div class="select-banner">
      <FileIcon size="128em" strokeWidth={0.5} />

      <h2>Click on any files to view details.</h2>
    </div>
  {/if}
</div>

<style lang="scss">
  div.file-details {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    border-radius: 0.5em;

    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-width: 320px;
    max-width: 320px;

    padding: 16px;

    overflow-y: auto;
    overflow-x: hidden;

    align-items: center;

    > div.select-banner {
      flex-grow: 1;

      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;

      gap: 32px;

      text-align: center;
    }

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

      min-width: 0px;
      max-width: 100%;

      > h2 {
        margin: 0px;
        text-align: center;

        overflow-x: hidden;
        white-space: nowrap;
        text-overflow: ellipsis;
        user-select: all;

        min-width: 0px;
      }

      > table {
        > tbody {
          > tr {
            > td {
              padding: 4px;

              > p {
                margin: 0px;
              }
            }

            > td.name {
              text-align: end;
            }

            > td.value {
              text-align: start;
            }
          }
        }
      }
    }
  }

  a.user-link {
    color: inherit;
    text-decoration: inherit;
  }

  a.user-link:hover {
    text-decoration: underline;
  }
</style>

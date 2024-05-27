<script lang="ts">
  import UserName from '$lib/client/user.svelte';
  import { Awaiter, LoadingSpinner } from '@rizzzi/svelte-commons';
  import { apiFetch } from '$lib/client.svelte';
  import type { FileResource } from '$lib/client/file';

  const { file }: { file: FileResource } = $props();
</script>

<div class="container">
  <div class="thumbnail">
    <img alt="Thumbnail" />
  </div>

  <p class="file-name">{file.name}</p>

  <div class="details">
    <div class="details-row">
      <p class="label">Created On</p>
      <p class="value">{new Date(file.createTime).toLocaleString()}</p>
    </div>

    {#if file.createTime != file.updateTime}
      <div class="details-row">
        <p class="label">Modified On</p>
        <p class="value">{new Date(file.updateTime).toLocaleString()}</p>
      </div>
    {/if}

    <div class="details-row">
      <p class="label">Owned By</p>
      <p class="value">
        <Awaiter
          callback={async () => {
            const user = await apiFetch({
              path: '/user/:' + file.domainUserId
            });

            return user;
          }}
        >
          {#snippet loading()}
            <LoadingSpinner size="1em" />
          {/snippet}
          {#snippet success({ result })}
            <UserName user={result} />
          {/snippet}
        </Awaiter>
      </p>
    </div>

    {#if file.domainUserId != file.authorUserId}
      <div class="details-row">
        <p class="label">Created By</p>
        <p class="value">
          <Awaiter
            callback={async () => {
              const user = await apiFetch({
                path: '/user/:' + file.authorUserId
              });

              return user;
            }}
          >
            {#snippet loading()}
              <LoadingSpinner size="1em" />
            {/snippet}
            {#snippet success({ result })}
              <UserName user={result} />
            {/snippet}
          </Awaiter>
        </p>
      </div>
    {/if}
  </div>
</div>

<style lang="scss">
  div.container {
    display: flex;
    flex-direction: column;
    gap: 16px;

    overflow: hidden auto;
    min-height: 0px;

    > div.thumbnail {
      min-width: 100%;
      aspect-ratio: 5/4;

      > img {
        min-width: 100%;
        min-height: 100%;

        border: 1px solid var(--shadow);

        box-sizing: border-box;
      }
    }

    > div.details {
      display: flex;
      flex-direction: column;
      gap: 8px;

      > div.details-row {
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: space-between;

        > p {
          min-width: 0px;
          overflow: hidden;

          text-overflow: ellipsis;
          text-wrap: nowrap;
        }

        > p.label {
          font-weight: bolder;
        }
        p.label::after {
          content: ':';
        }

        > p.value {
          text-align: right;

          flex-grow: 1;
        }
      }
    }

    > p.file-name {
      font-weight: bolder;
      text-align: center;
    }
  }
</style>

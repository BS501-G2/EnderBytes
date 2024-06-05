<script lang="ts">
  import UserName from '$lib/client/user.svelte';
  import { Awaiter, LoadingSpinner } from '@rizzzi/svelte-commons';
  import { getFileMimeType, getFileSize, getUser } from '$lib/client/api-functions';
  import type { File } from '$lib/server/db/file';
  import { FileType } from '$lib/shared/db';
  import { byteUnit } from '$lib/shared/utils';

  const { file }: { file: File } = $props();
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
        {#key file.id}
          <Awaiter
            callback={async () => {
              const user = await getUser(file.ownerUserId);

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
        {/key}
      </p>
    </div>

    {#if file.type === FileType.File}
      <div class="details-row">
        <p class="label">Size</p>
        <p class="value">
          {#key file.id}
            <Awaiter callback={async () => getFileSize(file)}>
              {#snippet loading()}
                <LoadingSpinner size="1em" />
              {/snippet}
              {#snippet success({ result })}
                {byteUnit(result)}
              {/snippet}
            </Awaiter>
          {/key}
        </p>
      </div>
      <div class="details-row">
        <p class="label">Type</p>
        <p class="value">
          {#key file.id}
            <Awaiter callback={async () =>await getFileMimeType(file, false)}>
              {#snippet loading()}
                <LoadingSpinner size="1em" />
              {/snippet}
              {#snippet success({ result })}
                {result}
              {/snippet}
            </Awaiter>
          {/key}
        </p>
      </div>
    {/if}

    {#if file.ownerUserId != file.creatorUserId}
      <div class="details-row">
        <p class="label">Created By</p>
        <p class="value">
          {#key file.id}
            <Awaiter
              callback={async () => {
                const user = await getUser(file.creatorUserId);

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
          {/key}
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

<script lang="ts">
  import { page } from '$app/stores'
 
  let paths: [name: string, href: string][] = [
    ['Admin', `/app/admin`],
    ['Dashboard', '/app/dashboard'],
    ['My Files', '/app/files'],
    ['Favorites', '/app/favorites'],
    ['Shared', '/app/shared'],
    ['Trash', '/app/trash']
  ]
</script>

<style lang="postcss">
  :root {
    --panel-width: 320px;
  }

  div.left-panel {
    width: var(--panel-width);
    height: 100%;

    position: absolute;

    left: 0px;
    top: 0px;
  }
</style>

<div class="left-panel">
  <ul>
    {#each paths as pathEntry}
      <li>
        <a href={pathEntry[1]}>
          {#if !$page.url.pathname.startsWith(pathEntry[1])}
            {pathEntry[0]}
          {:else}
            <b>{pathEntry[0]}</b>
          {/if}
        </a>
      </li>
    {/each}
  </ul>
</div>
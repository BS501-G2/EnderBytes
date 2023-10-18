<script lang="ts">
  import { page } from '$app/stores'

  import { ImageIcon } from 'svelte-feather-icons'

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
    --panel-width: 172px;
    --panel-width-contracted: 80px;

    --panel-side-head-section-height: 72px;
    --panel-side-foot-section-height: 64px;
  }

  div.side-panel {
    width: var(--panel-width-contracted);
    height: 100%;

    position: absolute;

    left: 0px;
    top: 0px;

    background-color: var(--side-panel-background);

    overflow: hidden;

    transition-duration: 400ms;
    transition-timing-function: cubic-bezier(0,1,.5,1);
  }

  div.side-panel:hover {
    width: var(--panel-width);

    box-shadow: 4px 0px 8px var(--gray-3);
  }

  div.head-section {
    height: var(--panel-side-head-section-height);
  }

  div.nav-section {
    height: calc(100% - var(--panel-side-head-section-height) - var(--panel-side-foot-section-height));
  }

  div.nav-entry {
    height: max-content;

    padding: 16px 0px 0px 0px;
    margin: 16px 4px 16px 4px;

    white-space: nowrap;
    display: flex;

    transition-duration: --panel-side-hover-transition-duration;
  }
  
  div.nav-entry-active {
    color: var(--gray-1);

    background-color: var(--color-2);
    border-radius: 8px;
  }

  div.nav-entry-image {
  }

  div.nav-entry-image-flex-container {
    width: 72px;
    height: 32px;

    justify-content: center;
    align-items: center;

    display: flex;
    transition-duration: 400ms;
    transition-timing-function: cubic-bezier(0,1,.5,1);
  }

  div.side-panel:hover
  div.nav-entry-image-flex-container {
    width: 48px;
  }

  div.nav-entry-image > p {
    width: 100%;

    text-align: center;
    font-size: 9px;

    transition-duration: 400ms;
  }

  div.side-panel:hover
  div.nav-entry-image > p {
    opacity: 0;
  }

  div.nav-entry-label {
    height: 32px;

    opacity: 0;
    transition-duration: 400ms;
  }

  div.side-panel:hover
  div.nav-entry-label {
    opacity: 1;
  }

  div.nav-entry-label-flex-container {
    height: 100%;
    align-items: center;
    display: flex;
  }

  div.foot-section {
    height: var(--panel-side-foot-section-height);
  }
</style>

<div class="side-panel">
  <div class="head-section">

  </div>
  <div class="nav-section">
    <ul>
      {#each paths as pathEntry}
      <li class="nav-list-entry">
        <a href={pathEntry[1]}>
          <div class="nav-entry {$page.url.pathname.startsWith(pathEntry[1]) ? "nav-entry-active" : ""}">
            <div class="nav-entry-image">
              <div class="nav-entry-image-flex-container">
                <ImageIcon size="32rem"/>
              </div>
              <p>
                {#if $page.url.pathname.startsWith(pathEntry[1])}
                <b>{pathEntry[0]}</b>
                {:else}
                {pathEntry[0]}
                {/if}
              </p>
            </div>

            <div class="nav-entry-label">
              <div class="nav-entry-label-flex-container">
                <p>
                  {#if $page.url.pathname.startsWith(pathEntry[1])}
                  <b>{pathEntry[0]}</b>
                  {:else}
                  {pathEntry[0]}
                  {/if}
                </p>
              </div>
            </div>
          </div>
        </a>
      </li>
      {/each}
    </ul>
  </div>
  <div class="foot-section">

  </div>
</div>
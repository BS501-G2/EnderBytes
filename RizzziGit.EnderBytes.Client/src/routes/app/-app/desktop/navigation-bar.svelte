<script lang="ts" context="module">
  import { page } from '$app/stores';
  import { navigating } from '$app/stores';
</script>

<script lang="ts">
  import {
    GridIcon,
    FolderIcon,
    UserIcon,
    TrashIcon,
    ShareIcon,
    StarIcon
  } from 'svelte-feather-icons';

  interface NavigationEntry {
    name: string;
    icon: any;
    path: string;
  }

  const navigationEntries: NavigationEntry[] = [
    { name: 'Feed', icon: GridIcon, path: 'feed' },
    { name: 'My Files', icon: FolderIcon, path: 'files' },
    { name: 'Starred', icon: StarIcon, path: 'starred' },
    { name: 'Shared', icon: ShareIcon, path: 'shared' },
    { name: 'Trash', icon: TrashIcon, path: 'trash' },
    { name: 'Users', icon: UserIcon, path: 'users' }
  ];
</script>

<div class="navigation-bar">
  {#each navigationEntries as entry}
    <a href={$navigating ? null : `${'/app/' + entry.path}`}>
      <div class="nav-entry {entry.path == $page.url.pathname.split('/')[2] ? 'active' : ''}">
        <svelte:component this={entry.icon} size="24"></svelte:component>
        <p>{entry.name}</p>
      </div>
    </a>
  {/each}
</div>

<style lang="scss">
  div.navigation-bar {
    min-width: 4em;
    max-width: 4em;

    display: flex;

    flex-direction: column;
    align-items: last;

    gap: 8px;

    flex-grow: 1;

    overflow-y: auto;
    // justify-content: safe center;

    > a {
      text-decoration: none;
      -webkit-app-region: no-drag;

      > div.nav-entry {
        display: flex;

        flex-direction: column;
        align-items: center;
        align-content: end;

        gap: 8px;

        color: var(--onPrimaryContainer);

        padding: 8px;
        border-radius: 8px;

        cursor: pointer;

        > p {
          flex-grow: 1;
          font-size: 0.8em;

          text-align: center;

          margin: 0px;
        }
      }

      > div.nav-entry.active {
        background-color: var(--primary);
        color: var(--onPrimary);
      }
    }
  }
</style>

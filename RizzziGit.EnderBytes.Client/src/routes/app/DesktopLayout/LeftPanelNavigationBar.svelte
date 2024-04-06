<script lang="ts" context="module">
  import { page } from "$app/stores";
  import { RootState } from "$lib/states/root-state";
</script>

<script lang="ts">
  import {
    GridIcon,
    FolderIcon,
    UserIcon,
    TrashIcon,
    ShareIcon,
    StarIcon,
  } from "svelte-feather-icons";

  interface NavigationEntry {
    name: string;
    icon: any;
    path: string;
  }

  const navigationEntries: NavigationEntry[] = [
    { name: "Feed", icon: GridIcon, path: "feed" },
    { name: "My Files", icon: FolderIcon, path: "files" },
    { name: "Starred", icon: StarIcon, path: "starred" },
    { name: "Shared", icon: ShareIcon, path: "shared" },
    { name: "Trash", icon: TrashIcon, path: "trash" },
    { name: "Profile", icon: UserIcon, path: "profile" },
  ];

  const rootState = RootState.state;
</script>

<div class="navigation-bar">
  {#each navigationEntries as entry}
    <a href="/app/{entry.path}">
      <div
        class="nav-entry {entry.path == $page.url.pathname.split('/')[2]
          ? 'active'
          : ''}"
      >
        <svelte:component this={entry.icon}></svelte:component>
        <p>{entry.name}</p>
      </div>
    </a>
  {/each}
</div>

<style lang="scss">
  div.navigation-bar {
    display: flex;

    flex-direction: column;
    align-items: last;

    gap: 8px;

    flex-grow: 1;

    overflow-y: auto;

    > a {
      text-decoration: none;

      > div.nav-entry {
        display: flex;

        flex-direction: row;
        align-items: center;
        align-content: end;

        gap: 8px;

        background-color: var(--primaryContainer);
        color: var(--onPrimaryContainer);

        padding: 8px;
        border-radius: 8px;

        user-select: none;
        cursor: pointer;

        > p {
          flex-grow: 1;

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

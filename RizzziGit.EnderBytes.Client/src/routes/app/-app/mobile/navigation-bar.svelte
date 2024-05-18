<script lang="ts">
  import { goto } from '$app/navigation';
  import { Button, ButtonClass } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';

  interface NavigationEntry {
    name: string;
    icon: string;
    path: string;
  }

  const navigationEntries: NavigationEntry[] = [
    { name: 'Feed', icon: 'fa-solid fa-house', path: 'feed' },
    { name: 'Files', icon: 'fa-solid fa-folder', path: 'files' },
    { name: 'Profile', icon: 'fa-solid fa-users', path: 'users?id=!me' }
  ];

  let entry: Writable<NavigationEntry> = writable(navigationEntries[0]);
</script>

<div class="navigation-bar">
  {#each navigationEntries as navigationEntry}
    <Button
      buttonClass={$entry == navigationEntry ? ButtonClass.Primary : ButtonClass.PrimaryContainer}
      outline={false}
      onClick={() => {
        goto(`/app/${navigationEntry.path}`);
        $entry = navigationEntry;
      }}
    >
      <div class="button">
        <i class={navigationEntry.icon}></i>
        <span>{navigationEntry.name}</span>
      </div>
    </Button>
  {/each}
</div>

<style lang="scss">
  div.navigation-bar {
    display: flex;
    flex-direction: row;
    justify-content: space-evenly;

    div.button {
      display: flex;
      flex-direction: column;
      align-items: center;

      padding: 4px 16px;
      gap: 4px;

      > i {
        font-size: 1.5em;
      }

      > span {
        font-size: 1em;
      }
    }
  }
</style>

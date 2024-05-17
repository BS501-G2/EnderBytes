<script lang="ts" context="module">
  import { introNavigationEntries, introNavigationButtons } from '$lib/intro-navigation';
</script>

<script lang="ts">
  import { goto } from '$app/navigation';
  import { onMount } from 'svelte';
  import { ExternalLinkIcon } from 'svelte-feather-icons';

  let scroll: number = 0;
  let hash: string = '';

  onMount(() => {
    if (hash.length == 0) {
      hash = '#home';
      goto('#home', { replaceState: true });
    }
  });
</script>

<svelte:window
  bind:scrollY={scroll}
  on:hashchange={() => {
    hash = location.hash;
  }}
/>

<div class="navigation-bar" style={scroll == 0 ? '' : ''}>
  <a href="/">
    <img src="/favicon.svg" alt="Site Logo" />
  </a>
  <div>
    <nav>
      <ul>
        {#each introNavigationEntries as homeNavigationEntry}
          <a
            class={homeNavigationEntry.path == hash ? 'active' : ''}
            href={homeNavigationEntry.path}
            ><li class="nav-entry">
              {#if !homeNavigationEntry.path.startsWith('#')}
                <ExternalLinkIcon size="20em" />
              {/if}
              <p>{homeNavigationEntry.name}</p>
            </li></a
          >
        {/each}
      </ul>

      <div>
        {#each introNavigationButtons as introNavigationButton}
          {#if hash != introNavigationButton.path}
            <button
              on:click={() => {
                hash = introNavigationButton.path;
                goto(introNavigationButton.path);
              }}>{introNavigationButton.name}</button
            >
          {/if}
        {/each}
      </div>
    </nav>
  </div>
</div>

<style lang="scss">
  ::-webkit-scrollbar {
    visibility: hidden;
  }

  div.navigation-bar {
    background-color: var(--primaryContainer);
    color: var(--onPrimaryContainer);

    display: flex;
    justify-content: center;
    align-items: center;

    width: 100vw;

    position: fixed;
    left: 0px;
    top: 0px;

    overflow-x: auto;

    box-shadow: #7f7f7f7f 0px 2px 8px;

    padding: 0px 32px 0px 32px;
    box-sizing: border-box;

    // @media only screen and (max-width: 1280px) {
    //  padding: 0px;
    // }

    > a {
      padding: 8px;
      width: 48px;
      height: 48px;

      > img {
        width: 100%;
        height: 100%;

        image-rendering: pixelated;

        filter: drop-shadow(#7f7f7f7f 2px 2px 2px);
      }
    }

    > div {
      display: flex;

      flex-grow: 1;

      max-width: 1280px;

      > nav {
        display: flex;

        flex-grow: 1;

        justify-content: center;
        align-items: center;

        > ul {
          list-style: none;

          flex-grow: 1;

          display: flex;

          margin: 0px 0px 0px 0px;
          padding-left: 0px;

          > a {
            padding: 0px 16px 0px 16px;

            margin: 0px 8px 0px 8px;
            border-radius: 32px;

            color: inherit;
            text-decoration: unset;

            > li {
              display: flex;

              align-items: center;

              > p {
                margin: 12px;
              }
            }
          }

          > a.active {
            background-color: var(--primary);
            color: var(--onPrimary);
          }
        }

        > div {
          height: 100%;

          > button {
            background-color: var(--primary);
            color: var(--onPrimary);

            height: 100%;

            border-style: solid;
            border-color: #00000000;
            border-radius: 8px;

            font-size: 14px;
            padding: 0px 16px 0px 16px;
            transition: cubic-bezier(0.075, 0.82, 0.165, 1);
            transition-duration: 200ms;

            margin: 0px 8px 0px 8px;

            cursor: pointer;
          }

          > button:nth-child(2) {
            background-color: transparent;
            color: var(--primary);
          }

          > button:hover {
            border-color: var(--onPrimary);
          }
        }
      }
    }
  }
</style>

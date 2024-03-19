<script lang="ts" context="module">
  import { Theme } from '$lib/themes'
  import { ViewMode } from '$lib/view-mode'
  import { STATE_ROOT } from '$lib/values'

  export interface RootState {
    theme: Theme

    viewMode: ViewMode
  }
</script>

<script lang="ts">
  import { APP_NAME, APP_TAGLINE } from '$lib/values'
  import { setContext, onMount } from 'svelte'
  import { writable } from 'svelte/store'
  import { serializeThemeColorsIntoInlineStyle } from '$lib/themes'

  const rootState = writable<RootState>({
    theme: Theme.Ender,
    viewMode: ViewMode.DesktopBrowser
  })

  setContext(STATE_ROOT, rootState)

  function onResize() {
    $rootState.viewMode = (
      (window.matchMedia('(max-width: 720px)').matches ? ViewMode.Mobile : ViewMode.Desktop) |
      (
        window.matchMedia('(display-mode: standalone)').matches ? ViewMode.Standalone :
        window.matchMedia('(display-mode: fullscreen)').matches ? ViewMode.Fullscreen :
        ViewMode.Browser
      )
    )
  }

  onMount(() => {
    rootState.subscribe((value) => document.documentElement.setAttribute('style', serializeThemeColorsIntoInlineStyle(value.theme)))

    $rootState.theme = (<Theme | null> localStorage.getItem('theme')) ?? Theme.Ender

    onResize()
  })
</script>

<svelte:head>

  <title>{APP_NAME} - {APP_TAGLINE}</title>
</svelte:head>

<svelte:window on:resize={onResize}/>

<slot />

<style lang="scss">
  :root {
    font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;

    background-color: var(--background);

    color: var(--onBackground);

    min-width: 320px;
  }
</style>

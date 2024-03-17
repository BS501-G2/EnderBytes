<script lang="ts" context="module">
  import { Theme } from '$lib/themes'

  export interface RootState {
    theme: Theme
  }
</script>

<script lang="ts">
  import { APP_NAME, APP_TAGLINE } from '$lib/manifest'
  import { setContext, onMount } from 'svelte'
  import { writable } from 'svelte/store'
  import { serializeThemeColorsIntoInlineStyle } from '$lib/themes'

  const rootState = writable<RootState>({
    theme: Theme.Default
  })

  setContext('state', rootState)

  onMount(() => {
    rootState.subscribe((value) => document.body.setAttribute('style', serializeThemeColorsIntoInlineStyle(value.theme)))

    $rootState.theme = (<Theme | null> localStorage.getItem('theme')) ?? Theme.Default

    setInterval(() => {
      $rootState.theme = $rootState.theme === Theme.Blue ? Theme.Default : Theme.Blue
    }, 1000)
  })
</script>

<svelte:head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link rel="manifest" href="/api/manifest.json" />
  <link rel="shortcut icon" href="/favicon.png" />

  <title>{APP_NAME} - {APP_TAGLINE}</title>
</svelte:head>

<slot />

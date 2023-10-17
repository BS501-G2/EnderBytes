<style global lang="postcss">
  /* Write your global styles here, in PostCSS syntax */
  @tailwind base;
  @tailwind components;
  @tailwind utilities;
</style>

<slot />

<svelte:head>
  <link rel="stylesheet" href="/styles/themes/{globalThis?.localStorage?.getItem('theme') ?? 'default'}.css">
</svelte:head>
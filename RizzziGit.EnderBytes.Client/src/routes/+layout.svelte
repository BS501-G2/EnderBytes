<style global lang="postcss">
  /* Write your global styles here, in PostCSS syntax */
  @tailwind base;
  @tailwind components;
  @tailwind utilities;

  :root {
    color: var(--gray-1);
    /* background-color: var(--gray-2); */
  }
</style>

<slot />

<svelte:head>
  <link rel="stylesheet" href="/styles/colors.css">
  <link rel="stylesheet" href="/styles/themes/{globalThis?.localStorage?.getItem('theme') ?? 'default'}.css">
</svelte:head>
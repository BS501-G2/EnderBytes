<script lang="ts">
  import { ResponsiveLayout, ViewMode, viewMode } from '@rizzzi/svelte-commons';
  const {}: {} = $props();
</script>

<div class="container{$viewMode & ViewMode.Mobile ? ' mobile' : ''}">
  <ResponsiveLayout>
    {#snippet desktop()}
    <div class="banner">
      BANNER
    </div>
    {/snippet}
  </ResponsiveLayout>

  <div class="form{$viewMode & ViewMode.Mobile ? ' mobile' : ''}">
    FORM
  </div>
</div>

<style lang="scss">
  :global(body) {
    display: flex;
    flex-direction: row;
    justify-content: safe center;
  }

  div.container {
    max-width: 1280px;
    min-width: 0px;

    flex-grow: 1;
    display: flex;
    flex-direction: row;

    align-items: stretch;

    padding: 16px;
    gap: 16px;

    > div.banner,
    > div.form {
      background-color: var(--backgroundVariant);
      color: var(--onBackgroundVariant);
    }

    > div.banner {
      flex-grow: 1;
    }

    > div.form {
      min-width: 320px;
      max-width: 320px;
    }

    > div.form.mobile {
      min-width: 0px;
      max-width: unset;
      flex-grow: 1;
    }
  }

  div.container.mobile {
    padding: unset;
  }
</style>

<script lang="ts" context="module">
  import { LocaleKey, getString } from '$lib/locale.svelte';
  import { RootState } from '$lib/states/root-state';
  import { _getUrl as faviconSrc } from '../../../../favicon.svg/+server';
</script>

<script lang="ts">
  import TitleBarChip from './chip.svelte';

  const rootState = RootState.state;
</script>

<TitleBarChip>
  <div class="chip">
    <img
      class="title-image"
      src={faviconSrc(16)}
      alt={getString(LocaleKey.AltIconSite)}
    />

    <p class="title-text">{getString(LocaleKey.AppName)}</p>
  </div>
</TitleBarChip>

<style lang="scss">
  div.chip {
    padding: 8px;
    display: flex;
    gap: 4px;

    align-items: center;

    img.title-image {
      max-height: 16px;
      aspect-ratio: 1;
    }

    -webkit-app-region: drag;

    p.title-text {
      margin: 0px;
      font-size: 14px;
      font-weight: lighter;

      color: var(--onPrimary);
    }
  }
</style>

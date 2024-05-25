<script lang="ts">
  import Locale, { LocaleKey, getDerivedString } from '$lib/locale.svelte';

  import { _getUrl as faviconSrc } from '../../../favicon.svg/+server';

  const string_AltIconSite = getDerivedString(LocaleKey.AltIconSite);
</script>

<div class="site-banner">
  <img class="title-image" src={faviconSrc(32)} alt={$string_AltIconSite} />

  <Locale string={[[LocaleKey.AppName]]} />
</div>

<style lang="scss">
  div.site-banner {
    width: 100%;

    padding: 32px;
    box-sizing: border-box;

    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: center;
    gap: 8px;

    font-weight: 100;
    font-size: 32px;
    line-height: 0px;

    color: var(--primaryVariant);

    > img.title-image {
      min-height: 32px;
      min-width: 32px;
      max-height: 32px;
      max-width: 32px;
    }
  }
</style>

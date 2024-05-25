<script lang="ts" context="module">
  import { LocaleKey, getDerivedString } from '$lib/locale.svelte';
  import { _getUrl as faviconSrc } from '../../../../favicon.svg/+server';
</script>

<script lang="ts">
  import Locale from '$lib/locale.svelte';
  import TitleBarChip from './chip.svelte';

  const stringAltIcon = getDerivedString(LocaleKey.AltIconSite);
</script>

<TitleBarChip>
  <div class="chip">
    <img class="title-image" src={faviconSrc(16)} alt={$stringAltIcon} />

    <p class="title-text">
      <Locale string={[[LocaleKey.AppName]]} />
    </p>
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

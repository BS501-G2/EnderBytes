<script lang="ts">
  import { LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";

  import { _getUrl as faviconSrc } from "../../routes/dynamic-icons/[size]/favicon.svg/+server";

  const rootState = RootState.state;
</script>

<div class="site-banner">
  <img src={faviconSrc(32)} alt={$rootState.getString(LocaleKey.AltIconSite)} />

  {$rootState.getString(LocaleKey.AppName)}
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
  }
</style>

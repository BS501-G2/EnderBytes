<script lang="ts" context="module">
  import { LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";
    import { _getUrl as faviconSrc } from "../../../dynamic-icons/[size]/favicon.svg/+server";
</script>

<script lang="ts">
  import Chip from "./TitleBarChip.svelte";

  const rootState = RootState.state;
  const appState = $rootState.appState;
</script>

<Chip>
  <img
    class="title-image"
    src={faviconSrc(16)}
    alt={$rootState.getString(LocaleKey.AltIconSite)}
  />

  <p class="title-text">{$rootState.getString(LocaleKey.AppName)}</p>
</Chip>

<style lang="scss">
  img.title-image {
    height: 100%;
  }

  p.title-text {
    margin: 0px;

    font-size: 14px;
    font-weight: 100;

    color: var(--onPrimary);
  }
</style>

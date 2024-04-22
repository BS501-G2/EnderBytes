<script lang="ts" context="module">
  import { LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";
  import { _getUrl as faviconSrc } from "../../../../routes/dynamic-icons/[size]/favicon.svg/+server";
</script>

<script lang="ts">
  import TitleBarChip from "./TitleBarChip.svelte";

  const rootState = RootState.state;
</script>

<TitleBarChip>
  <div class="chip">
    <img
      class="title-image"
      src={faviconSrc(16)}
      alt={$rootState.getString(LocaleKey.AltIconSite)}
    />

    <p class="title-text">{$rootState.getString(LocaleKey.AppName)}</p>
  </div>
</TitleBarChip>

<style lang="scss">
  div.chip {
    padding: 8px;
    display: flex;
    gap: 4px;

    align-items: center;

    img.title-image {
      height: 100%;
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

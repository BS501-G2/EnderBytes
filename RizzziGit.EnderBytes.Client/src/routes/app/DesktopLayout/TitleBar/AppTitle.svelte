<script lang="ts" context="module">
  import { LocaleKey } from "$lib/locale";
  import { getContext } from "svelte";
  import type { RootState } from "../../../+layout.svelte";
  import { STATE_APP, STATE_ROOT } from "$lib/values";
  import type { Readable, Writable } from "svelte/store";
  import type { AppState } from "../../+layout.svelte";
</script>

<script lang="ts">
  import Chip from "./TitleBarChip.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);
  const appState = getContext<Writable<AppState>>(STATE_APP);
</script>

<Chip>
  <img
    class="title-image"
    src="/favicon.svg?size=16"
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
  }
</style>

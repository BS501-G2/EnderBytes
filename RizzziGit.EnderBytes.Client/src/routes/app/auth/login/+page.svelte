<script lang="ts" context="module">
  import { LocaleKey } from "$lib/locale";
  import { RootState } from "$lib/states/root-state";
  import { ViewMode } from "$lib/view-mode";

  import SiteBanner from "./SiteBanner.svelte";
  import Banner from "./Banner.svelte";
  import { fetchAndInterpret } from "../../../../components/Bindings/Client.svelte";
</script>

<script lang="ts">
  const rootState = RootState.state;

  let enabled: boolean = true;

  let username: string;
  let password: string;

  async function onActivity(act: () => Promise<void>) {
    try {
      enabled = false;

      await act();
    } finally {
      enabled = true;
    }
  }

  let errorMessage: string | null = null;

  async function onSubmit(username: string, password: string) {
    try {
      errorMessage = null;

      await fetchAndInterpret('/auth/password-login', 'POST', { username, password });
    } catch (error: any) {
      errorMessage = error.message;

      throw error;
    }
  }
</script>

<div class="content">
  <div class="title-bar" />

  <div class="login-page">
    {#if $rootState.viewMode & ViewMode.Desktop}
      <svg class="banner-area">
        <Banner></Banner>
      </svg>
    {/if}
    <div
      class="form-area {$rootState.viewMode & ViewMode.Mobile
        ? 'form-area-mobile'
        : ''}"
    >
      <SiteBanner />

      <form
        on:submit={async (e) => {
          e.preventDefault();

          await onActivity(() => onSubmit(username, password));
        }}
      >
        {#if errorMessage != null}
          <p class="error-message">{errorMessage}</p>
        {/if}

        <div class="field">
          <label for="-username"
            >{$rootState.getString(
              LocaleKey.AuthLoginPageUsernamePlaceholder,
            )}</label
          >
          <input
            type="text"
            id="-username"
            name="username"
            placeholder={$rootState.getString(
              LocaleKey.AuthLoginPageUsernamePlaceholder,
            )}
            bind:value={username}
            disabled={!enabled}
          />
        </div>
        <div class="field">
          <label for="-password"
            >{$rootState.getString(
              LocaleKey.AuthLoginPagePasswordPlaceholder,
            )}</label
          >
          <input
            type="password"
            id="-password"
            name="password"
            placeholder={$rootState.getString(
              LocaleKey.AuthLoginPagePasswordPlaceholder,
            )}
            bind:value={password}
            disabled={!enabled}
          />
        </div>
        <div class="field">
          <button disabled={!enabled}
            >{$rootState.getString(LocaleKey.AuthLoginPageSubmit)}</button
          >
        </div>
      </form>
    </div>
  </div>
</div>

<style lang="scss">
  div.content {
    margin: auto;

    height: 100vh;

    display: flex;
    flex-direction: column;

    align-items: center;

    > div.title-bar {
      -webkit-app-region: drag;

      width: 100%;
      height: env(titlebar-area-height);

      background-color: var(--primaryContainer);
    }

    > div.login-page {
      width: 100%;
      max-width: 1280px;

      flex-grow: 1;

      display: flex;

      flex-direction: row;

      > svg.banner-area {
        flex-grow: 1;

        height: 100%;
      }

      > div.form-area {
        width: 320px;
        min-width: 320px;
        height: 100%;

        overflow-y: auto;

        background-color: var(--primaryContainer);

        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;

        > form {
          width: 100%;
          max-width: 420px;

          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;

          padding: 8px 32px 8px 32px;
          box-sizing: border-box;

          gap: 16px;

          > p.error-message {
            padding: 8px;
            margin: 0px;

            min-width: 100%;
            max-width: 100%;

            box-sizing: border-box;

            background-color: var(--error);
            color: var(--onError);
          }

          > div.field {
            width: 100%;

            > label {
              color: var(--onPrimary);
            }

            > input,
            > button {
              width: 100%;

              box-sizing: border-box;

              border: none;
              outline: none;

              font-size: 18px;
              padding: 8px;

              transition: all linear 150ms;
            }

            > input {
              border-style: solid;
              border-color: transparent;
              border-width: 1px;
            }

            > input:focus {
              border-color: var(--primary);
            }

            > button {
              background-color: var(--primary);
              color: var(--onPrimary);
            }

            > button:hover {
              cursor: pointer;

              background-color: var(--onPrimary);
              color: var(--primary);
            }
          }
        }
      }

      > div.form-area-mobile {
        width: unset;

        flex-grow: 1;

        > form {
          max-width: unset;
        }
      }
    }
  }
</style>

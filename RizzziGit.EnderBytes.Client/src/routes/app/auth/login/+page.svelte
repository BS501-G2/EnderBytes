<script lang="ts" context="module">
	import { LocaleKey, getString } from '$lib/locale.svelte';

	import SiteBanner from './site-banner.svelte';
	import Banner from './banner.svelte';
	import { apiFetch } from '$lib/client.svelte';
</script>

<script lang="ts">
	import { ResponsiveLayout, viewMode, ViewMode } from '@rizzzi/svelte-commons';

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

			await apiFetch({
				path: '/auth/password-login',
				method: 'POST',
				data: {
					username,
					password
				}
			});
		} catch (error: any) {
			errorMessage = error.message;

			throw error;
		}
	}
</script>

<div class="content">
	<div class="title-bar"></div>

	<div class="login-page">
		<ResponsiveLayout>
			{#snippet desktop()}
				<svg class="banner-area">
					<Banner></Banner>
				</svg>
			{/snippet}
		</ResponsiveLayout>
		<div class="form-area {$viewMode & ViewMode.Mobile ? 'form-area-mobile' : ''}">
			<div class="form-content">
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
						<input
							type="text"
							id="-username"
							name="username"
							placeholder={getString(LocaleKey.AuthLoginPageUsernamePlaceholder)}
							bind:value={username}
							disabled={!enabled}
						/>
					</div>
					<div class="field">
						<input
							type="password"
							id="-password"
							name="password"
							placeholder={getString(LocaleKey.AuthLoginPagePasswordPlaceholder)}
							bind:value={password}
							disabled={!enabled}
						/>
					</div>
					<div class="field">
						<button disabled={!enabled}>{getString(LocaleKey.AuthLoginPageSubmit)}</button>
					</div>
				</form>
			</div>
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
			-webkit-app-region: no-drag;

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
				max-width: 320px;
				min-width: 320px;
				height: 100%;

				overflow-y: auto;

				padding: 16px;
				box-sizing: border-box;

				display: flex;
				flex-direction: column;
				align-items: center;
				justify-content: center;

				> div.form-content {
					background-color: var(--backgroundVariant);
					border-radius: 16px;
					flex-grow: 1;

					max-width: 100%;
					min-width: 0px;

					display: flex;
					flex-direction: column;
					align-items: center;
					justify-content: center;

					> form {
						width: 100%;
						min-width: 0px;
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

							overflow-x: hidden;
							text-wrap: nowrap;
							max-lines: 1;
							text-overflow: ellipsis;

							box-sizing: border-box;

							background-color: var(--error);
							color: var(--onError);
						}

						> div.field {
							width: 100%;

							border: solid 1px rgba(0, 0, 0, 0.25);
							color: var(--onBackgroundVariant);

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
			}

			> div.form-area-mobile {
				width: unset;

				flex-grow: 1;
			}
		}
	}
</style>

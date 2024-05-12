<script lang="ts">
	import { page } from '$app/stores';

	import { RootState } from '$lib/states/root-state';
	import { getLocale } from '$lib/locale.svelte';

	import DesktopLayout from './-app/desktop.svelte';
	import MobileLayout from './-app/mobile.svelte';
	import ResponsiveLayout from '$lib/responsive-layout.svelte';
	import AccountSettingsDialog from './-app/account-settings-dialog.svelte';
	import LogoutConfirmationDialog from './-app/logout-confirmation-dialog.svelte';
	import AppSettingsDialog from './-app/app-settings-dialog.svelte';
	import Awaiter from '$lib/awaiter.svelte';
	import Client, { apiFetch } from '$lib/client.svelte';
	import LoadingSpinnerPage from '$lib/widgets/loading-spinner-page.svelte';

	const rootState = RootState.state;
</script>

<svelte:head>
	<link rel="manifest" href="/manifest.json?locale={getLocale()}&theme={$rootState.theme}" />
</svelte:head>

{#if $page.url.pathname.startsWith('/app/auth')}
	<slot />
{:else}
	<ResponsiveLayout>
		{#snippet desktop()}
			<DesktopLayout>
				<slot />
			</DesktopLayout>
		{/snippet}
		{#snippet mobile()}
			<MobileLayout>
				<slot />
			</MobileLayout>
		{/snippet}
	</ResponsiveLayout>

	<LogoutConfirmationDialog />
	<AccountSettingsDialog />
	<AppSettingsDialog />
	<!-- <Awaiter callback={() => apiFetch({ path: '/user/!me' })}>
		<svelte:fragment slot="success" let:result={user}>
			<ResponsiveLayout>
				{#snippet desktop()}
					<DesktopLayout>
						<slot />
					</DesktopLayout>
				{/snippet}
				{#snippet mobile()}
					<MobileLayout>
						<slot />
					</MobileLayout>
				{/snippet}
			</ResponsiveLayout>

			<LogoutConfirmationDialog />
			<AccountSettingsDialog />
			<AppSettingsDialog />
		</svelte:fragment>
		<svelte:fragment slot="loading">
			<div class="loading" style="display: flex; width: 100vw; height: 100vh;">
				<LoadingSpinnerPage />
			</div>
		</svelte:fragment>
	</Awaiter> -->
{/if}

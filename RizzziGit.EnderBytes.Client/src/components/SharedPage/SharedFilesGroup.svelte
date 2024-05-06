<script lang="ts">
	import { FileIcon, FolderIcon } from 'svelte-feather-icons';
	import Awaiter from '../Bindings/Awaiter.svelte';
	import { apiFetch } from '../Bindings/Client.svelte';

	export let fileAccesses: any[];
</script>

<Awaiter callback={() => apiFetch({ path: `/user/:${fileAccesses[0].authorUserId}` })}>
	<svelte:fragment slot="success" let:result={user}>
		<div class="entry">
			<img class="profile-picture" src="/favicon.svg" alt="User Profile" />
			<div class="details">
				<div class="header">
					<p class="username">
						<a class="subject" href="/app/users/:{user.id}">
							{user.firstName}{user.middleName ? ` ${user.middleName[0]}.` : ''}
							{user.lastName}
						</a>
						<span class="predicate"> shared to you. </span>
					</p>
					<p class="time">100 years ago</p>
				</div>

				<div class="file-list">
					{#each fileAccesses as fileAccess}
						<Awaiter callback={() => apiFetch({ path: `/file/:${fileAccess.targetFileId}` })}>
							<svelte:fragment slot="success" let:result={file}>
								<a class="file-entry" href="/app/files/{file.id}">
									<div class="icon">
										{#if file.isFolder}
											<FolderIcon size="40em" strokeWidth={1} />
										{:else}
											<FileIcon size="40em" strokeWidth={1} />
										{/if}
									</div>
									<div class="info">
										<p class="name">{file.name}</p>
									</div>
								</a>

								<!-- <p class="file">
									<a class="subject" href="/app/files/:{file.id}">{file.name}</a>
								</p> -->
							</svelte:fragment>
							<svelte:fragment slot="loading">
								<p hidden class="file">Loading...</p>
							</svelte:fragment>
						</Awaiter>
					{/each}
				</div>
			</div>
		</div>
	</svelte:fragment>
</Awaiter>

<style lang="scss">
	a.subject {
		color: inherit;
		font-weight: bold;
		text-decoration: none;
	}

	a.subject:hover {
		text-decoration: underline;
	}

	div.entry {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);

		// min-width: calc(520px - 3em - 48px);
		// max-width: calc(520px - 3em - 48px);

		padding: 16px;

		border-radius: 8px;

		display: flex;
		flex-direction: row;
		gap: 8px;

		> img.profile-picture {
			min-width: 48px;
			min-height: 48px;
			max-width: 48px;
			max-height: 48px;
		}

		> div.details {
			flex-grow: 1;

			display: flex;
			flex-direction: column;
			gap: 8px;

			> div.header {
				display: flex;
				gap: 8px;

				> p.username {
					font-size: 1em;
					line-height: 1em;
					flex-grow: 1;

					> span.predicate {
						font-style: italic;
						font-weight: lighter;
					}
				}

				> p.time {
					font-weight: lighter;
					font-style: italic;
					font-size: 0.75em;
				}
			}

			> div.file-list {
				display: flex;
				flex-direction: row;
				flex-wrap: wrap;
				align-items: flex-start;
				gap: 8px;

				> a.file-entry {
					display: flex;
					flex-direction: row;
					align-items: center;
					gap: 8px;
					text-decoration: none;

					background-color: var(--background);
					color: var(--onBackground);

					padding: 16px;

					border-radius: 8px;
					> div.info {
						display: flex;
						flex-direction: column;
						gap: 4px;

						> p.name {
							font-weight: bolder;
						}

						> p {
							line-height: 1em;
						}
					}
				}
				> a.file-entry:hover {
					> div.info {
						> p.name {
							text-decoration: underline;
						}
					}
				}
			}
		}
	}
</style>

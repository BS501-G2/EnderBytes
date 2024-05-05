<script lang="ts">
	import type { SharedFile } from '../SharedPage.svelte';

	export let sharedFile: SharedFile;
</script>

<div class="entry">
	<img class="profile-picture" src="/favicon.svg" alt="User Profile" />
	<div class="details">
		<p class="username">
			<a class="subject" href="/app/users/:{sharedFile.author.id}">{sharedFile.author.firstName}</a>
			<span class="predicate"
				>shared {sharedFile.files.length} file{sharedFile.files.length > 1 ? 's' : ''}</span
			>
		</p>

		<p>asd</p>
	</div>
</div>

<style lang="scss">
	div.entry {
		padding: 16px;

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

			> p.username {
				font-size: 1em;
				line-height: 1em;

				> a.subject {
					color: inherit;
					font-weight: bold;
					text-decoration: none;
				}

				> a.subject:hover {
					text-decoration: underline;
				}

				> span.predicate {
					font-style: italic;
					font-weight: lighter;
				}
			}
		}
	}
</style>

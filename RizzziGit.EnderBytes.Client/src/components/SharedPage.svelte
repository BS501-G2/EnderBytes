<script lang="ts" context="module">
	export interface SharedFile {
		author: any;
		files: any[];
	}
</script>

<script lang="ts">
	import SharedEntry from './SharedPage/SharedEntry.svelte';

	async function loading() {}

	let sharedFiles: SharedFile[] = [
		{
			author: {
				username: 'test',
				firstName: 'test',
				lastName: 'test',
				middleName: 'test'
			},
			files: [
				{
					name: 'test'
				}
			]
		}
	];
	let nextPage: boolean = true;

	$: sharedFilesCount = sharedFiles.map((file) => file.files.length).reduce((a, b) => a + b, 0);
</script>

<div class="page">
	<div class="header">
		<h1>Shared Files</h1>
	</div>

	<div class="body">
		{#each sharedFiles as sharedFile}
			<SharedEntry {sharedFile} />
		{/each}
	</div>
</div>

<style lang="scss">
	div.page {
		padding: 32px;

		display: flex;
		flex-direction: column;
		flex-grow: 1;
		gap: 32px;

		max-height: 100%;
		> div.body {
			overflow-y: auto;
			overflow-x: hidden;
		}
	}
</style>

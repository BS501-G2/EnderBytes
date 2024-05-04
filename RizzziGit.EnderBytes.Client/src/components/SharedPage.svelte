<script lang="ts">
	async function loading() {}
</script>

<div class="page">
	<div class="header">
		<h1>Shared Files</h1>
	</div>

	<div class="body">
		{#each (function* () {
			for (let index = 0; index < 1000; index++) {
				yield `${Math.random()}`;
			}
		})() as entry}
			<p>{entry}</p>
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

    overflow-y: auto;

    max-height: 100%;
	}
</style>

<script lang="ts" context="module">
	export const enabled: Writable<boolean> = writable(false);
	export const onUploadCompleteListeners: Writable<Array<() => void>> = writable([]);
</script>

<script lang="ts">
	import Axios from 'axios';

	import Awaiter from '../Bindings/Awaiter.svelte';
	import Button, { ButtonClass } from '../Widgets/Button.svelte';
	import Dialog from '../Widgets/Dialog.svelte';
	import { writable, type Writable } from 'svelte/store';
	import { createFile } from '../FileBrowser.svelte';

	export let currentFileId: number | null;

	let fileInput: HTMLInputElement;

	let files: FileList | null = null;

	let load: () => Promise<void>;
</script>

<input type="file" bind:files multiple hidden bind:this={fileInput} />

{#if $enabled}
	<Dialog onDismiss={() => ($enabled = false)}>
		<h2 slot="head">Upload</h2>
		<div class="body" slot="body">
			<p style="margin-top: 0px">
				The uploaded files will be put inside the current folder. Alternatively, you can drag and
				drop files on the folder's area.
			</p>
		</div>
		<svelte:fragment slot="actions">
			<Awaiter
				callback={async () => {
					if (files == null || files.length == 0) {
						throw new Error('No files selected');
					}

					const promises = Array.from(files).map(async (file) => createFile(currentFileId, file));

					files = null;
					$enabled = false;

					await Promise.all(promises);
					$onUploadCompleteListeners.forEach((callback) => callback());
				}}
				autoLoad={false}
				bind:load
			>
				<svelte:fragment slot="not-loaded">
					<div class="button">
						<Button
							onClick={() => {
								fileInput.click();
							}}
							buttonClass={ButtonClass.Background}
						>
							{#if (files?.length ?? 0) >= 1}
								{Array.from(files ?? [])
									.map((file) => file.name)
									.join(', ')}
							{:else}
								<p>Cilck here to select files.</p>
							{/if}
						</Button>
					</div>

					<div class="button">
						<Button onClick={load}>Upload</Button>
					</div>
					<div class="button">
						<Button onClick={() => ($enabled = false)}>Cancel</Button>
					</div>
				</svelte:fragment>
			</Awaiter>
		</svelte:fragment>
	</Dialog>
{/if}

<style lang="scss">
	div.body {
		display: flex;
		flex-direction: column;
		gap: 8px;
	}

	div.button {
		display: flex;
		flex-direction: column;
	}

	div.button:nth-child(1) {
		flex-grow: 100;
	}
	div.button:nth-child(2) {
		flex-grow: 1;
	}
</style>

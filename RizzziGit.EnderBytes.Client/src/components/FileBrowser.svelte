<script lang="ts" context="module">
	export interface BaseFileBrowserInformation {
		isFolder: boolean;
		current: any;
		pathChain: { isSharePoint: boolean; root: any; chain: any[] };
		access: { accessList: any[]; highestExtent: number };
	}

	export interface FileBrowserFolderInformation extends BaseFileBrowserInformation {
		isFolder: true;
	}

	export interface FileBrowserFileInformation extends BaseFileBrowserInformation {
		isFolder: false;
	}

	export type FileBrowserInformation = FileBrowserFolderInformation | FileBrowserFileInformation;

	export type FileBrowserSelection = Writable<any[]>;

	export async function valdiateFileName(
		parentFolderId: number | null,
		name: string
	): Promise<string> {
		const result = await apiFetch({
			path: `/file/${parentFolderId != null ? `:${parentFolderId}` : '!root'}/files/new-name-validation`,
			method: 'POST',
			data: { name }
		});

		if (result.hasIllegalCharacters) {
			throw new Error('Invalid file name');
		} else if (result.hasIllegalLength) {
			throw new Error('File name has invalid length');
		} else if (result.nameInUse) {
			throw new Error('File name already in use');
		}

		return name;
	}

	export async function autoIncrementName(
		parentFolderId: number | null,
		name: string
	): Promise<string> {
		let count = 1;

		const currentName = () => `${name}${count > 1 ? ` (${count})` : ''}`;

		while (true) {
			const result = await apiFetch({
				path: `/file/${parentFolderId != null ? `:${parentFolderId}` : '!root'}/files/new-name-validation`,
				method: 'POST',
				data: {
					name: currentName()
				}
			});

			if (result.hasIllegalCharacters || result.hasIllegalLength || result.nameInUse) {
				count++;
			} else {
				break;
			}
		}

		return currentName();
	}

	export async function createFolder(parentFolderId: number | null, name: string): Promise<any> {
		name = await valdiateFileName(parentFolderId, name);

		const result = <number>await executeBackgroundTask(
			`Folder Creation: ${name}`,
			true,
			async (_, setStatus) => {
				setStatus(`Creating...`);
				const result = await apiFetch({
					path: `/file/${parentFolderId != null ? `:${parentFolderId}` : '!root'}/files/new-folder`,
					method: 'POST',
					data: {
						name,
						isFolder: true
					}
				});
				setStatus(`Folder successfully created`);

				return result.file;
			},
			false
		).run();
		return result;
	}

	export async function createFile(parentFolderId: number | null, file: File): Promise<any> {
		const formData = new FormData();

		formData.append('offset', '0');
		formData.append('content', file, 'content');

		const name = await autoIncrementName(parentFolderId, file.name);

		// if (file.)

		const client = executeBackgroundTask<any>(
			`File upload: ${name}`,
			true,
			async (client, setStatus) => {
				const formData = new FormData();

				formData.append('name', name);
				formData.append('content', file, 'content');

				const result = await apiFetch({
					path: `/file/${parentFolderId != null ? `:${parentFolderId}` : '!root'}/files/new-file`,
					method: 'POST',
					data: formData,
					uploadProgress: (progress, total) => {
						if (progress == total) {
							setStatus('Uploading...', progress / total);
						} else {
							setStatus('Processing...', null);
						}
					}
				});

				setStatus('File successfully uploaded');
				return result.file;
			},
			false
		);

		await client.run();
	}
</script>

<script lang="ts">
	import DesktopLayout from './FileBrowser/DesktopLayout.svelte';
	import ResponsiveLayout from './Bindings/ResponsiveLayout.svelte';
	import FolderCreationDialog from './FileBrowser/FolderCreationDialog.svelte';
	import FileCreationDialog, {
		onUploadCompleteListeners
	} from './FileBrowser/FileCreationDialog.svelte';
	import { writable, type Writable } from 'svelte/store';
	import Awaiter, { type AwaiterResetFunction } from './Bindings/Awaiter.svelte';
	import { apiFetch } from './Bindings/Client.svelte';
	import { onDestroy, onMount } from 'svelte';
	import { executeBackgroundTask } from './BackgroundTaskList.svelte';

	export let currentFileId: number | null;

	const selection: FileBrowserSelection = writable([]);

	async function load(): Promise<FileBrowserInformation> {
		// await new Promise<void>((resolve) => setTimeout(resolve, 1000));
		// throw new Error();

		const id = currentFileId != null ? `:${currentFileId}` : '!root';
		const [current, pathChain, access] = await Promise.all([
			apiFetch({ path: `/file/${id}` }),
			apiFetch({ path: `/file/${id}/path-chain` }),
			apiFetch({ path: `/file/${id}/shares` })
		]);

		if (current.isFolder) {
			return { isFolder: true, current, pathChain, access };
		} else {
			return { isFolder: false, current, pathChain, access };
		}
	}

	let reset: AwaiterResetFunction;

	$: {
		currentFileId;

		$selection = [];
	}

	onMount(() => {
		$onUploadCompleteListeners.push(reset);
	});

	onDestroy(() => {
		const index = $onUploadCompleteListeners.indexOf(reset);

		if (index >= 0) {
			$onUploadCompleteListeners.splice(index, 1);
		}
	});
</script>

{#key currentFileId}
	<Awaiter callback={load} bind:reset>
		<svelte:fragment slot="loading">
			<ResponsiveLayout>
				<svelte:fragment slot="desktop">
					<DesktopLayout info={null} {reset} {selection} />
				</svelte:fragment>
			</ResponsiveLayout>
		</svelte:fragment>
		<svelte:fragment slot="success" let:result={info}>
			<ResponsiveLayout>
				<svelte:fragment slot="desktop">
					<DesktopLayout {info} {reset} {selection} />
				</svelte:fragment>
			</ResponsiveLayout>

			<FolderCreationDialog bind:currentFileId />
			<FileCreationDialog bind:currentFileId />
		</svelte:fragment>
	</Awaiter>
{/key}

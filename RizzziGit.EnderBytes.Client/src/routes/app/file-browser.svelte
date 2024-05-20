<script lang="ts" context="module">
  import { apiFetch } from '$lib/client.svelte';
  import { ResponsiveLayout } from '@rizzzi/svelte-commons';
  import type { ControlBarItem } from './-file-browser/desktop/main-panel/control-bar.svelte';

  export interface FileResource {
    id: number;
    createTime: number;
    updateTime: number;

    trashTime?: number;

    domainUserId: number;
    authorUserId: number;

    parentId?: number;
    name: string;

    isFolder: boolean;
  }

  export interface FileAccessResource {
    id: number;
    createTime: number;
    updateTime: number;

    authorUserId: number;
    targetEntityType: number;
    targetEntityId?: number;

    extent: number;
  }

  export interface FilePathChainInfo {
    root: FileResource;
    chain: FileResource[];
    isSharePoint: boolean;
  }

  export interface FileAccessListInfo {
    highestExtent: number;
    accessPoint: FileAccessPoint;
    accessList: FileAccessResource[];
  }

  export interface FileAccessPoint {
    accesspoint: FileAccessResource;
    pathChain: FileResource[];
  }

  export interface SharedFileListFilter {
    sort: 'name' | 'ctime' | 'utime';
    desc: boolean;
    offset: number;
    authorUserId?: number;
    domainUserId?: number;
    extent?: number;
  }

  export const files: Record<number, FileResource> = {};

  function storeFile(file: FileResource) {
    return file.id in files ? Object.assign(files[file.id], file) : (files[file.id] = file);
  }

  export async function getFile(id?: number, cache?: FileResource): Promise<FileResource> {
    const result: FileResource =
      cache ??
      (await apiFetch({
        path: id != null ? `/file/:${id}` : '/file/!root'
      }));

    return storeFile(result);
  }

  export async function getFilePathChain(file: FileResource): Promise<FilePathChainInfo> {
    const result: FilePathChainInfo = await apiFetch({
      path: `/file/:${file.id}/path-chain`
    });

    result.root = storeFile(result.root);
    result.chain = result.chain.map((file) => storeFile(file));

    return result;
  }

  export async function getFileAccessList(file: FileResource): Promise<FileAccessListInfo> {
    const result: FileAccessListInfo = await apiFetch({
      path: `/file/:${file.id}/shares`
    });

    if (result.accessPoint) {
      result.accessPoint.pathChain = result.accessPoint.pathChain.map((file) => storeFile(file));
    }

    return result;
  }

  export async function scanFolder(
    file: FileResource,
    params?: FolderListFilter
  ): Promise<FileResource[]> {
    const result: FileResource[] = await apiFetch({
      path: `/file/:${file.id}/files`,
      params: params as Record<string, string>
    });

    return result.map(storeFile);
  }

  export interface NewFileNameValidation {
    hasIllegalCharacters: boolean;
    hasIllegalLength: boolean;
    nameInUse: boolean;
  }

  export async function getNewNameValidationFlag(
    parentFolder: FileResource,
    name: string
  ): Promise<NewFileNameValidation> {
    const result: NewFileNameValidation = await apiFetch({
      path: `/file/:${parentFolder.id}/files/new-name-validation`,
      method: 'POST',
      data: {
        name
      }
    });

    return result;
  }

  export async function uploadFile(parentFolder: FileResource, file: File): Promise<FileResource> {
    const backgroundTask = executeBackgroundTask<FileResource>(
      'Upload File',
      false,
      async (_, setStatus) => {
        const validation = await getNewNameValidationFlag(parentFolder, file.name);

        if (validation.hasIllegalCharacters) {
          throw new Error('Illegal characters.');
        } else if (validation.hasIllegalLength) {
          throw new Error('Illegal length.');
        } else if (validation.nameInUse) {
          throw new Error('Name in use.');
        }

        setStatus(`Uploading file ${file.name}...`);

        const form = new FormData();

        form.append('content', file);
        form.append('name', file.name);

        const result: FileResource = await apiFetch({
          path: `/file/:${parentFolder.id}/files/new-file`,
          method: 'POST',
          data: form,
          uploadProgress(progress, total) {
            setStatus(`Uploading file ${file.name}...`, progress / total);
          }
        });

        setStatus(`Uploaded file ${file.name}.`);

        return result;
      },
      false
    );

    return await backgroundTask.run();
  }

  export async function createFolder(
    parentFolder: FileResource,
    newName: string
  ): Promise<FileResource> {
    const backgroundTask = executeBackgroundTask<FileResource>(
      'Create Folder',
      false,
      async (_, setStatus) => {
        const validation = await getNewNameValidationFlag(parentFolder, newName);

        if (validation.hasIllegalCharacters) {
          throw new Error('Illegal characters.');
        } else if (validation.hasIllegalLength) {
          throw new Error('Illegal length.');
        } else if (validation.nameInUse) {
          throw new Error('Name in use.');
        }

        setStatus(`Creating folder ${newName}...`);

        const result: FileResource = await apiFetch({
          path: `/file/:${parentFolder.id}/files/new-folder`,
          method: 'POST',
          data: {
            name: newName
          }
        });

        setStatus(`Created folder ${newName}.`);

        return result;
      },
      false
    );

    return await backgroundTask.run();
  }

  export type FileBrowserState = {
    title?: string;

    hidePathChain?: boolean;
    hideSidePanel?: boolean;

    controlBarActions?: ControlBarItem[];
  } & (
    | {
        isLoading: true;
      }
    | {
        isLoading: false;

        file: FileResource | null;
        access: FileAccessListInfo | null;
        pathChain: FilePathChainInfo | null;
        files: FileResource[];
      }
  );
</script>

<script lang="ts">
  import DesktopLayout from './-file-browser/desktop.svelte';
  import MobileLayout from './-file-browser/mobile.svelte';
  import type { FolderListFilter } from './files/arrange-overlay.svelte';
  import { executeBackgroundTask } from '$lib/background-task.svelte';
  import { writable, type Writable } from 'svelte/store';

  const { fileBrowserState }: { fileBrowserState: Writable<FileBrowserState> } = $props();
  const selection: Writable<FileResource[]> = writable([]);
</script>

<ResponsiveLayout>
  {#snippet desktop()}
    <DesktopLayout fileBrowserState={fileBrowserState as any} {selection} />
  {/snippet}
  {#snippet mobile()}
    <MobileLayout fileBrowserState={fileBrowserState as any} />
  {/snippet}
</ResponsiveLayout>

<style lang="scss">
</style>

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

    isFolder: string;
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

  export async function scanFolder(file: FileResource, params?: FolderListFilter): Promise<FileResource[]> {
    const result: FileResource[] = await apiFetch({
      path: `/file/:${file.id}/files`,
      params: params as Record<string, string>
    });

    return result.map(storeFile);
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
  import type { FolderListFilter } from './files/filter-overlay.svelte';

  let { fileBrowserState = $bindable() }: { fileBrowserState: FileBrowserState } = $props();

  let selection: FileResource[] = $state([]);
</script>

<ResponsiveLayout>
  {#snippet desktop()}
    <DesktopLayout {fileBrowserState} bind:selection />
  {/snippet}
  {#snippet mobile()}
    <MobileLayout {fileBrowserState} />
  {/snippet}
</ResponsiveLayout>

<style lang="scss">
</style>

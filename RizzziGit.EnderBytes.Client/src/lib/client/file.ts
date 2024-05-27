import { executeBackgroundTask } from '$lib/background-task.svelte';
import { apiFetch } from '$lib/client.svelte';
import type { FolderListFilter } from '../../routes/app/files/arrange-overlay.svelte';

export const files: Record<number, FileResource> = {};

function storeFile(file: FileResource) {
  return file.id in files ? Object.assign(files[file.id], file) : (files[file.id] = file);
}

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

export interface FileAccessPoint {
  accesspoint: FileAccessResource;
  pathChain: FileResource[];
}

export interface ShareFileListFilter {
  sort: 'name' | 'ctime' | 'utime';
  desc: boolean;
  offset: number;
  authorUserId?: number;
  domainUserId?: number;
  extent?: number;
}

export async function getFile(id?: number, cache?: FileResource): Promise<FileResource> {
  const result: FileResource =
    cache ??
    (await apiFetch({
      path: id != null ? `/file/:${id}` : '/file/!root'
    }));

  return storeFile(result);
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

export async function uploadFile(
  parentFolder: FileResource,
  ...file: File[]
): Promise<FileResource[]> {
  const backgroundTask = executeBackgroundTask<FileResource[]>(
    file.length > 1 ? `Upload ${file.length} files` : `Upload ${file[0].name}`,
    false,
    async (_, setStatus) => {
      const form = new FormData();

      for (let i = 0; i < file.length; i++) {
        form.append(`requestFiles`, file[i]);
      }

      const result: FileResource[] = await apiFetch({
        path: `/file/:${parentFolder.id}/files/new-file`,
        method: 'POST',
        data: form,
        uploadProgress(progress, total) {
          setStatus(`Uploading file content...`, progress / total);

          if (progress == total) {
          setStatus(`Processing...`);
          }
        }
      });

      setStatus(`Operation completed.`);
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

export interface ShareFileEntry {
  fileAccess: FileAccessResource;
  file: FileResource;
}

export async function getFileAccesses(params?: ShareFileListFilter): Promise<ShareFileEntry[]> {
  const result: ShareFileEntry[] = await apiFetch({
    path: '/shares',
    params: <never>params
  });

  return result.map(({ fileAccess, file }) => ({
    fileAccess,
    file: storeFile(file)
  }));
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

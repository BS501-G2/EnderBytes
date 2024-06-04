export enum UserRole {
  Member,
  SiteAdmin,
  SystemAdmin
}

export enum UserKeyType {
  Password,
  Session
}

export enum FileType {
  File,
  Folder,
  Link
}

export enum FileAccessLevel {
  None,
  Read,
  ReadWrite,
  Manage,
  Full
}

export const usernameLength: [min: number, max: number] = [6, 16] as const;
export const usernameValidCharacters = Object.freeze(
  'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.'
);

export enum UsernameVerificationFlag {
  OK = 0,
  InvalidCharacters = 1 << 0,
  InvalidLength = 1 << 1,
  Taken = 1 << 2
}

export const fileNameLength: [min: number, max: number] = [1, 256] as const;
export const fileNameInvalidCharacters = '\\/:*?\'"<>|';

export enum FileNameVerificationFlag {
  OK = 0,
  InvalidCharacters = 1 << 0,
  InvalidLength = 1 << 1,
  FileExists = 1 << 2
}

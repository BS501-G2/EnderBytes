export const waitForNextBulkReqestTimeout = 100;
export const maxRequestSizeLimit = 1024 * 1024 * 256;
export const maxBulkRequestEntryCount = 10000;
export const userSessionExpiryDuration = 30 * 24 * 60 * 60 * 1000;

export interface Authentication {
  userSessionId: number;
  userSessionKey: Uint8Array;
  userKeyId: number;
}

export enum ApiErrorType {
  Unknown,
  InvalidRequest,

  Unauthorized,
  Forbidden
}

export class ApiError extends Error {
  public static throw(
    status: ApiErrorType,
    message?: string,
    { cause, stack }: ApiErrorOptions = {}
  ): never {
    throw new ApiError(
      status,
      cause?.message ?? message ?? `${ApiErrorType[status]} (code ${status})`,
      { stack, cause }
    );
  }

  public static throwFrom(
    error: Error,
    status: ApiErrorType = ApiErrorType.Unknown,
    message?: string
  ): never {
    return ApiError.throw(status, message, { cause: error, stack: error.stack });
  }

  public constructor(
    status: ApiErrorType,
    message: string,
    { stack, cause }: ApiErrorOptions = {}
  ) {
    super(message, { cause });

    this.status = status;
    this.stack = stack;
  }

  public readonly status: ApiErrorType;
}

export interface ApiErrorOptions extends ErrorOptions {
  stack?: string;
  cause?: Error;
}

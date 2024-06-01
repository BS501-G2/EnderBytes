import { BSON } from 'bson';

import type { Map } from '$lib/server/api';
import {
  ApiError,
  ApiErrorType,
  maxBulkRequestEntryCount,
  maxRequestSizeLimit,
  waitForNextBulkReqestTimeout
} from '$lib/shared/api';
import { clearAuthentication } from './api-functions';

export type ApiRequest<T extends keyof Map> = [name: T, ...args: Parameters<Map[T]>];
export enum ApiResponseType {
  InvalidInvocationRequest,
  InvokeSuccess,
  InvokeError
}

export type ApiResponse =
  | [type: ApiResponseType.InvokeSuccess, data: any]
  | [
      type: ApiResponseType.InvokeError,
      status: ApiErrorType,
      name: string,
      message: string,
      stack?: string
    ]
  | [
      type: ApiResponseType.InvalidInvocationRequest,
      status: ApiErrorType,
      message: string,
      stack?: string
    ];

export interface RequestData extends BSON.Document {
  data: Uint8Array[];
}

export interface ResponseData extends BSON.Document {
  data: [true, Uint8Array[]] | [false, message: string];
}

class A extends Error {}

async function bulkRequest(requests: Uint8Array[]): Promise<Uint8Array[]> {
  const requestBody = BSON.serialize({
    data: requests
  } as RequestData);

  if (requestBody.length > maxRequestSizeLimit) {
    // throw new Error('Request too large');
    throw new A();
  }

  const request = new Request('/api', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/bson'
    },
    body: requestBody
  });

  const { data: response } = BSON.deserialize(
    new Uint8Array(await (await fetch(request)).arrayBuffer()),
    {
      promoteBuffers: true
    }
  ) as ResponseData;

  if (response[0]) {
    return response[1];
  } else {
    throw new Error(response[1]);
  }
}

type RequestQueueEntry = [
  requestData: Uint8Array,
  resolve: (data: Uint8Array) => void,
  reject: (reason?: any) => void
];
let requestQueue: RequestQueueEntry[] = [];
let requestQueueRunning: boolean = false;
let requestQueueWaitForNextTimeout: number = 0;

async function runQueue(): Promise<void> {
  if (requestQueueRunning) {
    return;
  }

  requestQueueRunning = true;
  try {
    while (requestQueue.length > 0) {
      requestQueueWaitForNextTimeout = Date.now() + waitForNextBulkReqestTimeout;

      const entries: RequestQueueEntry[] = [];
      const entriesSize = () => entries.reduce((size, entry) => size + entry[0].byteLength, 0);

      const skipped: RequestQueueEntry[] = [];
      while (
        requestQueue.length > 0 &&
        entriesSize() < maxRequestSizeLimit &&
        entries.length < maxBulkRequestEntryCount
      ) {
        const entry = requestQueue.shift();
        if (entry == null) {
          break;
        }

        if (entry[0].byteLength > maxRequestSizeLimit) {
          entry[2](new Error('Request too large'));
        } else if (entriesSize() + entry[0].byteLength <= maxRequestSizeLimit) {
          entries.push(entry);
        } else {
          skipped.push(entry);
        }

        if (entries.length < maxBulkRequestEntryCount && requestQueue.length === 0) {
          await new Promise<void>((resolve) =>
            setTimeout(resolve, requestQueueWaitForNextTimeout - Date.now())
          );
        }
      }
      requestQueue.push(...skipped);

      if (entries.length === 0) {
        continue;
      }

      while (true) {
        try {
          const mapped = entries.map((entry) => entry[0]);
          let data: Uint8Array[];

          try {
            data = await bulkRequest(mapped);
          } catch (error: any) {
            if (error instanceof A) {
              requestQueue.unshift(entries.pop()!);

              continue;
            } else {
              throw error;
            }
          }

          for (let i = 0; i < entries.length; i++) {
            entries[i][1](data[i]);
          }
          break;
        } catch (error: any) {
          for (let i = 0; i < entries.length; i++) {
            entries[i][2](error);
          }
          break;
        }
      }
    }
  } finally {
    requestQueueRunning = false;
  }
}

export async function clientSideInvoke<T extends keyof Map>(
  name: T,
  ...args: Parameters<Map[T]>
): Promise<Awaited<ReturnType<Map[T]>>> {
  const result = BSON.deserialize(
    new Uint8Array(
      await new Promise<Uint8Array>((resolve, reject) => {
        requestQueue.unshift([
          BSON.serialize({ data: [name, ...args] as ApiRequest<T> }),
          resolve,
          reject
        ]);
        runQueue();
      })
    ),
    {
      promoteBuffers: true
    }
  ).data as ApiResponse;

  if (result[0] === ApiResponseType.InvokeError) {
    const error = Object.assign(new ApiError(result[1], result[2], { stack: result[3] }), {
      name: ApiResponseType[result[0]]
    });

    if (error.status === ApiErrorType.Unauthorized) {
      clearAuthentication();
    }

    throw error;
  } else if (result[0] === ApiResponseType.InvalidInvocationRequest) {
    throw new ApiError(result[1], result[2], { stack: result[3] });
  } else {
    return result[1];
  }
}

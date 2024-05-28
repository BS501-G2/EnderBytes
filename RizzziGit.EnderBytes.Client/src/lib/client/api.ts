import { BSON } from 'bson';

import type { Map } from '$lib/server/api';
import { requestSizeLimit } from '$lib/shared/values';

export type ApiRequest<T extends keyof Map> = [name: T, ...args: Parameters<Map[T]>];
export enum ApiResponseType {
  InvalidInvocationRequest,
  InvokeSuccess,
  InvokeError
}

export type ApiResponse =
  | [type: ApiResponseType.InvokeSuccess, data: any]
  | [type: ApiResponseType.InvokeError, name: string, message: string, stack?: string]
  | [type: ApiResponseType.InvalidInvocationRequest, message: string, stack?: string];

export interface RequestData extends BSON.Document {
  data: Uint8Array[];
}

export interface ResponseData extends BSON.Document {
  data: [true, Uint8Array[]] | [false, message: string];
}

async function bulkRequest(requests: Uint8Array[]): Promise<Uint8Array[]> {
  const request = new Request('/api', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/bson'
    },
    body: BSON.serialize({
      data: requests
    } as RequestData)
  });

  const { data: response } = BSON.deserialize(
    new Uint8Array(await (await fetch(request)).arrayBuffer())
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
let requestQueueDeadline: number = 0;

async function runQueue(): Promise<void> {
  if (requestQueueRunning) {
    return;
  }

  requestQueueRunning = true;
  try {
    while (requestQueue.length > 0) {
      requestQueueDeadline = Date.now() + 500;

      const entries: RequestQueueEntry[] = [];
      const entriesSize = () => entries.reduce((size, entry) => size + entry[0].byteLength, 0);

      while (requestQueue.length > 0 && entriesSize() < requestSizeLimit && entries.length < 500) {
        const entry: RequestQueueEntry = requestQueue.shift()!;

        if (entry[0].byteLength > requestSizeLimit) {
          entry[2](new Error('Request too large'));
        } else if (entriesSize() + entry[0].byteLength <= requestSizeLimit) {
          entries.push(entry);
        } else {
          requestQueue.unshift(entry);
        }
      }

      if (entries.length > 0) {
        await new Promise<void>((resolve) =>
          setTimeout(resolve, requestQueueDeadline - Date.now())
        );
      }

      if (entries.length === 0) {
        continue;
      }

      try {
        const mapped = entries.map((entry) => entry[0]);
        const data = (await bulkRequest(mapped)).map((buffer) => new Uint8Array(buffer.buffer));
        for (let i = 0; i < entries.length; i++) {
          entries[i][1](data[i]);
        }
      } catch (error: any) {
        for (let i = 0; i < entries.length; i++) {
          entries[i][2](error);
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
        requestQueue.push([
          BSON.serialize({ data: [name, ...args] as ApiRequest<T> }),
          resolve,
          reject
        ]);
        runQueue();
      })
    )
  ).data as ApiResponse;

  if (result[0] === ApiResponseType.InvokeError) {
    throw Object.assign(new Error(result[2]), {
      name: result[1],
      stack: result[3]
    });
  } else if (result[0] === ApiResponseType.InvalidInvocationRequest) {
    throw new Error(result[1]);
  } else {
    return result[1];
  }
}
import type { RequestEvent } from '@sveltejs/kit';
import { BSON } from 'bson';

import { invoke, functionExists } from '$lib/server/api';
import { Database } from '$lib/server/db';
import {
  ApiResponseType,
  type ApiRequest,
  type ApiResponse,
  type RequestData,
  type ResponseData
} from '$lib/client/api';
import { ApiError, ApiErrorType, maxRequestSizeLimit } from '$lib/shared/api';

async function processRequest(raw: Uint8Array): Promise<Uint8Array> {
  const result = await (async (): Promise<ApiResponse> => {
    if (raw.length > maxRequestSizeLimit) {
      return [ApiResponseType.InvalidInvocationRequest, ApiErrorType.InvalidRequest, 'Request too large'];
    } else if (raw.length === 0) {
      return [ApiResponseType.InvalidInvocationRequest, ApiErrorType.InvalidRequest, 'Empty request'];
    }

    let request: ApiRequest<any>;
    try {
      request = BSON.deserialize(raw, { promoteBuffers: true }).data as ApiRequest<any>;
    } catch (error: any) {
      if (error instanceof ApiError) {
        return [ApiResponseType.InvalidInvocationRequest, error.status, error.message];
      } else {
        return [ApiResponseType.InvalidInvocationRequest, ApiErrorType.InvalidRequest, error.message];
      }
    }

    const name = request[0];
    const args = request.slice(1);

    if (typeof name !== 'string') {
      return [ApiResponseType.InvalidInvocationRequest, ApiErrorType.InvalidRequest, `Invalid function name: ${name}`];
    }

    if (!functionExists(name as (typeof request)[0])) {
      return [ApiResponseType.InvalidInvocationRequest, ApiErrorType.InvalidRequest, `Unknown function: ${name}`];
    }

    try {
      return [ApiResponseType.InvokeSuccess, await invoke(request[0], ...request.slice(1))];
    } catch (error: any) {
      if (error instanceof ApiError) {
        return [ApiResponseType.InvokeError, error.status, error.name, error.rawMessage, error.stack]
      }

      return [ApiResponseType.InvokeError, ApiErrorType.Unknown, error.name, error.message, error.stack];
    }
  })();

  return BSON.serialize({ data: result });
}

export async function POST({ request }: RequestEvent): Promise<Response> {
  const database = await Database.getInstance();
  const response = await database.transact<ResponseData['data']>(async () => {
    const requestBuffer = await request.arrayBuffer();

    if (requestBuffer.byteLength === 0) {
      return [false, 'Empty Request'];
    } else if (requestBuffer.byteLength > maxRequestSizeLimit) {
      return [false, 'Request too large'];
    }

    let requests: Uint8Array[];

    try {
      requests = (
        BSON.deserialize(new Uint8Array(requestBuffer), {
          promoteBuffers: true
        }) as RequestData
      ).data;
    } catch (e: any) {
      return [false, e.message];
    }

    if (requests.reduce((request, value) => request + value.length, 0) > maxRequestSizeLimit) {
      return [false, 'Request too Large'];
    }

    return [true, await Promise.all(requests.map(processRequest))];
  });

  const data = BSON.serialize({ data: response } as ResponseData);
  return new Response(data, {
    headers: {
      'Content-Type': 'application/bson',
      'Content-Length': `${data.length}`
    }
  });
}

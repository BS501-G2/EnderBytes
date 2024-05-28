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
import { requestSizeLimit } from '$lib/shared/values';

async function processRequest(raw: Uint8Array): Promise<Uint8Array> {
  const result = await (async (): Promise<ApiResponse> => {
    if (raw.length > requestSizeLimit) {
      return [ApiResponseType.InvalidInvocationRequest, 'Request too large'];
    } else if (raw.length === 0) {
      return [ApiResponseType.InvalidInvocationRequest, 'Empty request'];
    }

    let request: ApiRequest<any>;
    try {
      request = BSON.deserialize(raw).data as ApiRequest<any>;
    } catch (error: any) {
      return [ApiResponseType.InvalidInvocationRequest, error.message];
    }

    if (!functionExists(request[0])) {
      return [ApiResponseType.InvalidInvocationRequest, `Unknown function: ${request[0]}`];
    }

    try {
      return [ApiResponseType.InvokeSuccess, await invoke(request[0], ...request.slice(1))];
    } catch (error: any) {
      return [ApiResponseType.InvokeError, error.name, error.message, error.stack];
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
    } else if (requestBuffer.byteLength > requestSizeLimit) {
      return [false, 'Request too large'];
    }

    let requests: Uint8Array[];

    try {
      requests = (BSON.deserialize(new Uint8Array(requestBuffer)) as RequestData).data.map(
        (request) => new Uint8Array(request.buffer)
      );
    } catch (e: any) {
      return [false, e.message];
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

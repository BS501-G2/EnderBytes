import type { RequestEvent } from '@sveltejs/kit';
import { BSON } from 'bson';

import {
  ApiResponseType,
  invoke,
  functionExists,
  type ApiRequest,
  type ApiResponse
} from '$lib/server/api';
import { Database } from '$lib/server/db';

export async function POST({ request }: RequestEvent): Promise<Response> {
  const database = await Database.getInstance();
  const response = await database.transact<ApiResponse>(async () => {
    let apiRequest: ApiRequest<any>;

    try {
      if (request.headers.get('Content-Type') !== 'application/bson') {
        return [ApiResponseType.InvalidInvocationRequest, 'Invalid content type'];
      }

      const reader = await request.arrayBuffer();
      if (reader == null || reader.byteLength === 0) {
        return [ApiResponseType.InvalidInvocationRequest, 'Empty request'];
      }

      apiRequest = BSON.deserialize(new Uint8Array(reader)).data;
    } catch {
      return [ApiResponseType.InvalidInvocationRequest, 'Invalid request'];
    }

    const [name, ...args] = apiRequest;
    if (!functionExists(name)) {
      return [ApiResponseType.InvalidInvocationRequest, `Unknown function: ${name}`];
    }

    return await invoke(name, ...args);
  });

  const data = BSON.serialize({ data: response });
  return new Response(data, {
    headers: {
      'Content-Type': 'application/bson',
      'Content-Length': `${data.length}`
    }
  });
}

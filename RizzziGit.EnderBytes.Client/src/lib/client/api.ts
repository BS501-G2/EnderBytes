import { BSON } from 'bson';

import type { Map } from '$lib/server/api';

export async function clientSideInvoke<T extends keyof Map>(
  name: T,
  ...args: Parameters<Map[T]>
): Promise<Awaited<ReturnType<Map[T]>>> {
  const request = new Request('/api', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/bson'
    },
    body: BSON.serialize({
      data: [name, ...args]
    })
  });

  const fetchResponse = await fetch(request);

  if (fetchResponse.headers.get('Content-Type') === 'application/bson') {
    const response = await fetchResponse.arrayBuffer();
    const result = BSON.deserialize(new Uint8Array(response)).data;

    return <never>result;
  } else {
    throw new Error('Invalid content type');
  }
}

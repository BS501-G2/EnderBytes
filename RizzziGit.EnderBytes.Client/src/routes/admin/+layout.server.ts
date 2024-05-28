import { getServerStatus } from '$lib/server/api-functions';
import { Database } from '$lib/server/db';
import { redirect, type RequestEvent } from '@sveltejs/kit';

export async function load(event: RequestEvent) {
  const database = await Database.getInstance();

  await database.transact(async () => {
    const status = await getServerStatus();

    if (status.setupRequired && event.url.pathname !== '/admin/setup') {
      redirect(302, '/admin/setup');
    }
  });
}

import { Database } from '$lib/server/db';
import type { User } from '$lib/server/db/user';
import { UserKeyManager, type UnlockedUserKey, type UserKey } from '$lib/server/db/user-key';

export interface UserSession {
  userId: number;

  userKey: UnlockedUserKey;
  payload: Buffer;
}

export class UserSessionManager {
  public constructor() {
    this.sessions = new Map();
  }

  public readonly sessions: Map<number, UserSession>;

  public async isValid(user: User, userKey: UnlockedUserKey, payload: Buffer): Promise<boolean> {
    const database = await Database.getInstance();
    const userKeyManager = database.getManager(UserKeyManager);

    const testUserKey = await userKeyManager.get(userKey.id);
    if (testUserKey == null) {
      return false;
    }

    try {
      userKeyManager.unlock(testUserKey, payload);
      return true;
    } catch {
      return false;
    }
  }

  private currentId: number = 0;
  public get nextId(): number {
    return this.currentId++;
  }

  public async create(user: User, userKey: UserKey, payload: Buffer): Promise<number | null> {
    try {
      const database = await Database.getInstance();
      const userKeyManager = database.getManager(UserKeyManager);
      const unlockedUserKey = await userKeyManager.unlock(userKey, payload);

      const { nextId } = this;

      this.sessions.set(nextId, {
        userId: user.id,
        userKey: unlockedUserKey,
        payload
      });

      return nextId;
    } catch {
      return null;
    }
  }
}

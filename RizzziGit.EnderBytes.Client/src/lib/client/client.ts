import { EventEmitter, type EventInterface } from '@rizzzi/eventemitter'

export interface Session {
  userId: number
  token: string
}

export interface ClientEvent extends EventInterface {
  stateChange: [state: number]
  sessionChange: [session?: Session]
}

export interface PromiseObject {
  func: () => any | Promise<any>,
  resolve: (...args: any) => void,
  reject: (...args: any) => void
}

export class ClientError extends Error {
  public constructor(response: Response) {
    super(`HTTP ${response.status}: ${response.statusText}`)

    this.response = response
  }

  readonly response: Response

  public get error(): Response { return this.response }
}

type ClientEventMap = {
  [P in keyof ClientEvent]: (...args: ClientEvent[P]) => (Promise<void> | void)
}

export class Client {
  static readonly STATE_NOT_CONNECTED = 0;
  static readonly STATE_CONNECTING = 1;
  static readonly STATE_READY = 2;
  static readonly STATE_BORKED = 3;

  static #initializing: boolean = false
  static #client: Client | null = null

  public static async getInstance(url: URL, events: ClientEventMap): Promise<Client> {
    return Client.#client ??= await (async () => {
      try {
        const { STATE_NOT_CONNECTED, STATE_CONNECTING, STATE_READY, STATE_BORKED } = this

        Client.#initializing = true

        const client = new Client(await (<any>window).__DOTNET__INIT__({
          STATE_NOT_CONNECTED: () => STATE_NOT_CONNECTED,
          STATE_READY: () => STATE_READY,
          STATE_CONNECTING: () => STATE_CONNECTING,
          STATE_BORKED: () => STATE_BORKED,

          WS_URL: () => url.toString(),

          OnStateChange: (state: number) => client!.#emit('stateChange', state),
          OnSessionTokenChange: (session: string) => client!.#emit('sessionChange', client!.#sessionCache = JSON.parse(session))
        }))

        for (const eventName in events) {
          client!.on(eventName, events[eventName])
        }

        await client.#run()

        return client
      }
      finally {
        Client.#initializing = false
      }
    })()
  }

  public constructor(dotnet: any) {
    if (!Client.#initializing) {
      throw new Error('Private constructor.')
    }

    this.#events = new EventEmitter({
      requireErrorHandling: false
    })
    this.#dotnet = dotnet.RizzziGit.EnderBytes.Client.Library.Client

    void this.#emit('ready')
  }

  readonly #dotnet: any
  readonly #events: EventEmitter<ClientEvent>
  get state(): number { return this.#dotnet.GetState() }
  get session(): Session | null { return JSON.parse(this.#dotnet.GetSession()); }

  #tasks: PromiseObject[] = []
  #currentTask?: PromiseObject
  #queueRunning: boolean = false
  #sessionCache: Session | null = null

  async #runQueue(): Promise<void> {
    if (this.#queueRunning) {
      return
    }

    this.#queueRunning = true
    try {
      while ((this.#currentTask ??= this.#tasks.shift()) != null) {
        const { func, resolve, reject } = this.#currentTask

        try {
          resolve(await func())
        }
        catch (error: any) {
          if (error?.message?.includes('Invalid state to send request.')) {
            await this.#dotnet.Run();
            continue;
          }

          reject(error)
        }

        this.#currentTask = undefined
      }
    }
    finally {
      this.#queueRunning = false
    }
  }

  async #run() {
    await this.#dotnet.Run();
    void this.#runQueue()

    if (this.#sessionCache != null) {
      const { userId, token } = this.#sessionCache

      await this.authenticateByToken(userId, token)
    }
  }

  #queue<T>(func: () => (T | Promise<T>)): Promise<T> {
    const promise = new Promise<T>((resolve, reject) => {
      this.#tasks.push({ func, resolve, reject })
    })

    void this.#runQueue()

    return promise
  }

  authenticateByPassword(username: string, password: string): Promise<boolean> {
    return this.#queue(() => this.#dotnet.AuthenticateByPassword(username, password))
  }

  authenticateByToken(userId: number, token: string): Promise<boolean> {
    return this.#queue(() => this.#dotnet.AuthenticateByToken(`${userId}`, token))
  }

  destroyToken(): Promise<boolean> {
    return this.#queue(() => this.#dotnet.DestroyToken())
  }

  getToken(): Promise<Session | null> {
    return this.#queue(async () => JSON.parse(this.#dotnet.GetToken()))
  }

  randomBytes(length: number): Promise<Uint8Array> {
    return this.#queue(async () => Uint8Array.from(atob(await this.#dotnet.RandomBytes(length)), (e) => e.charCodeAt(0)))
  }

  // authenticateWithPassword(username: string, password: string): Promise<Session | null> {
  //   return this.#queue(async () => JSON.parse(await this.#dotnet.AuthenticateWithPassword(username, password)))
  // }

  // validate(session: Session): Promise<boolean> {
  //   return this.#queue(async () => await this.#dotnet.Validate(`${session.userId}`, session.token))
  // }

  // setSession(session: Session | null): Promise<void> {
  //   return this.#queue(async () => {
  //     if (session == null) {
  //       this.#dotnet.SetSession("null")
  //       return;
  //     }

  //     if (await this.#dotnet.Validate(`${session.userId}`, session.token)) {
  //       this.#dotnet.SetSession(JSON.stringify(session ?? null))
  //     }
  //   })
  // }

  get #emit() { return this.#events.bind().emit }
  get on() { return this.#events.bind().on }
  get off() { return this.#events.bind().off }
  get once() { return this.#events.bind().once }
}

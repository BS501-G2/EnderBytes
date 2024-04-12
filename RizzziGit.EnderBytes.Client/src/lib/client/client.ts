import { EventEmitter, type EventInterface } from '@rizzzi/eventemitter'

export interface Session {
  userId: number
  token: string
}

export interface ClientEvent extends EventInterface {
  stateChange: [state: number]
  sessionChange: [session: Session | null]
}

export interface PromiseObject {
  func: () => any | Promise<any>,
  resolve: (...args: any) => void,
  reject: (...args: any) => void
}

export class ClientError extends Error {
  public constructor(responseCode: number, responseCodeString: string) {
    super(`Server response: ${responseCodeString}(${responseCode})`)

    this.responseCode = responseCode
    this.responseCodeString = responseCodeString
  }

  readonly responseCode: number
  readonly responseCodeString: string
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
  static #clientPromise: Promise<Client> | null = null
  static #assembly: any

  public static async getInstance(url: URL, events: ClientEventMap): Promise<Client> {
    try {
      return Client.#client ??= await (this.#clientPromise ??= (async () => {
        try {
          const { STATE_NOT_CONNECTED, STATE_CONNECTING, STATE_READY, STATE_BORKED } = this

          Client.#initializing = true

          while (!('__DOTNET__INIT__' in window)) {
            await new Promise<void>((resolve) => setTimeout(resolve, 100))
          }

          const client = new Client(this.#assembly ??= await (<any>window).__DOTNET__INIT__({
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

          Object.assign(window, { client })

          return client
        }
        finally {
          Client.#initializing = false
        }
      })())
    }
    finally {
      Client.#clientPromise = null
    }
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
  get state(): number {
    try {
      return this.#dotnet.GetState()
    } catch {
      return Client.STATE_BORKED
    }
  }
  public get session(): Session | null { return this.#session }

  #tasks: PromiseObject[] = []
  #currentTask?: PromiseObject
  #queueRunning: boolean = false
  #sessionCache: Session | null = null
  #session: Session | null = null

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

      await this.loginToken(userId, token)
    }
  }

  #queue<T>(func: () => (T | Promise<T>)): Promise<T> {
    const promise = new Promise<T>((resolve, reject) => {
      this.#tasks.push({ func, resolve, reject })
    })

    void this.#runQueue()

    return promise
  }

  #getRequestInt(request: string): number | null {
    return this.#dotnet.GetRequestInt(request)
  }

  #getRequestString(request: number): string | null {
    return this.#dotnet.GetRequestString(request)
  }

  #getResponseInt(response: string): number | null {
    return this.#dotnet.GetResponseInt(response)
  }

  #getResponseString(response: number): string | null {
    return this.#dotnet.GetResponseString(response)
  }

  #request(request: string | number, requestData: ArrayBuffer | any = null): Promise<ArrayBuffer | any> {
    if (typeof (request) === 'string') {
      request = this.#getRequestInt(request)!

      if (request == null) {
        throw new Error('Invalid request string.')
      }
    }

    return this.#queue(async () => {
      let responseCode: number | null = null
      if (requestData instanceof ArrayBuffer) {
        responseCode = await this.#dotnet.SendRawRequest(request, new Uint8Array(requestData))
      } else {
        responseCode = await this.#dotnet.SendJsonRequest(request, JSON.stringify(requestData))
      }

      if (responseCode == null) {
        throw new Error("Unknown request/response data.")
      } else if (responseCode != this.#dotnet.GetResponseInt("Okay")) {
        throw new ClientError(responseCode, this.#getResponseString(responseCode)!)
      }

      const responseType = this.#dotnet.GetResponseType()
      let responseData: ArrayBuffer | any

      if (responseType == 0) {
        responseData = JSON.parse(this.#dotnet.ReceiveJsonResponse())
      } else if (responseType == 1) {
        responseData = (this.#dotnet.ReceiveRawResponse()).buffer
      }

      return responseData
    })
  }

  public echo<T>(message: T): Promise<T> {
    return this.#request('Echo', message)
  }

  public async loginPassword(username: string, password: string): Promise<void> {
    const session = await this.#request('LoginPassword', { username, password })

    this.#emit('sessionChange', this.#session = session)
  }

  public async loginToken(userId: number, sessionToken: string): Promise<void> {
    const session = await this.#request('LoginToken', { userId, token: sessionToken })

    this.#emit('sessionChange', this.#session = session)
  }

  public async register(username: string, password: string, lastName: string, firstName: string, middleName: string | null): Promise<number> {
    return await this.#request('Register', { username, password, lastName, firstName, middleName })
  }

  public async logout(): Promise<void> {
    const session = this.#session

    if (session == null) {
      return
    }

    await this.#request('Logout', session)
    this.#emit('sessionChange', this.#session = null)
  }

  public async getOwnStorage(): Promise<any> {
    return await this.#request('GetOwnStorage')
  }

  public async getOwnStorageId(): Promise<number> {
    return (await this.getOwnStorage()).Id
  }

  public async resolveUserId(username: string): Promise<number | null> {
    return await this.#request('ResolveUserId', { username })
  }

  public async getUser(userId: number): Promise<any> {
    return await this.#request('GetUser', { userId })
  }

  public async getLoginUser(): Promise<any> {
    if (this.session == null) {
      return
    }
    return await this.getUser(this.session!.userId)
  }

  public async scanFolder(folderId: number | null): Promise<number[]> {
    return await this.#request('ScanFolder', { folderId })
  }

  public async getRootFolderId(): Promise<number> {
    return await this.#request('GetRootFolderId')
  }

  public async getFile(fileId: number): Promise<any> {
    return await this.#request('GetFile', { fileId })
  }

  public async createFolder(name: string, folderId: number | null): Promise<number> {
    return await this.#request('Create', { name, folderId, isFolder: true })
  }

  public async createFile(name: string, folderId: number | null): Promise<number> {
    return await this.#request('Create', { name, folderId, isFolder: false })
  }

  get #emit() { return this.#events.bind().emit }
  get on() { return this.#events.bind().on }
  get off() { return this.#events.bind().off }
  get once() { return this.#events.bind().once }
}

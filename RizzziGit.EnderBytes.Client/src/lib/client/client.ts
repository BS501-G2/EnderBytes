import { dev } from '$app/environment'
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
  public constructor(responseCode: number, responseCodeString: string, errorMessage: string) {
    super(`Server response: ${responseCodeString}(${responseCode}): ${errorMessage}`)

    this.responseCode = responseCode
    this.responseCodeString = responseCodeString
    this.errorMessage = errorMessage
  }

  readonly responseCode: number
  readonly responseCodeString: string
  readonly errorMessage: string
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


            SetState: (state: number) => client.#state = state,
            GetState: () => { return client.#state },
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
  #state: number = Client.STATE_NOT_CONNECTED
  get state(): number { return this.#state }
  public get session(): Session | null { return this.#session }

  #sessionCache: Session | null = null
  #session: Session | null = null

  async #run() {
    await this.#dotnet.Run();

    if (this.#sessionCache != null) {
      const { userId, token } = this.#sessionCache

      await this.loginToken(userId, token)
    }
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

  async #request(request: string | number, requestData: ArrayBuffer | any = null): Promise<ArrayBuffer | any> {
    if (typeof (request) === 'string') {
      request = this.#getRequestInt(request)!

      if (request == null) {
        throw new Error('Invalid request string.')
      }
    }

    if (dev) {
      // await new Promise<void>((resolve) => setTimeout(resolve, Math.floor(Math.random() * 1000)))
    }

    while (true) {
      try {

        const responseId: number = requestData instanceof ArrayBuffer
          ? await this.#dotnet.SendRawRequest(request, new Uint8Array(requestData))
          : await this.#dotnet.SendJsonRequest(request, JSON.stringify(requestData))

        const responseCode = this.#dotnet.GetResponseCode(responseId)

        if (responseCode == null) {
          throw new Error("Unknown request/response data.")
        } else if (responseCode != this.#dotnet.GetResponseInt("Okay")) {
          throw new ClientError(responseCode, this.#getResponseString(responseCode)!, new TextDecoder().decode(this.#dotnet.ReceiveRawResponse(responseId).buffer))
        }

        const responseType = this.#dotnet.GetResponseType(responseId)
        let responseData: ArrayBuffer | any

        if (responseType == 0) {
          responseData = JSON.parse(this.#dotnet.ReceiveJsonResponse(responseId))
        } else if (responseType == 1) {
          responseData = (this.#dotnet.ReceiveRawResponse(responseId)).buffer
        }

        return responseData
      }
      catch (error: any) {
        if (error?.message?.includes('Invalid state to send request.')) {
          await this.#dotnet.Run();
          continue;
        }

        throw error
      }
    }
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

  public async getFile(fileId: number | null = null): Promise<any> {
    return await this.#request('GetFile', { fileId: fileId ?? await this.getRootFolderId() })
  }

  public async createFolder(name: string, folderId: number | null): Promise<number> {
    return await this.#request('Create', { name, folderId, isFolder: true })
  }

  public async createFile(name: string, folderId: number | null): Promise<number> {
    return await this.#request('Create', { name, folderId, isFolder: false })
  }

  get #emit() { return this.#events.bind().emit }
  public get on() { return this.#events.bind().on }
  public get off() { return this.#events.bind().off }
  public get once() { return this.#events.bind().once }
}

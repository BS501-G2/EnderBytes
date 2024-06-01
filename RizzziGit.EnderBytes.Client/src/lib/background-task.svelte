<script lang="ts" context="module">
  import { authentication } from '$lib/client/api-functions';
  import { writable, get, type Writable, type Readable, derived } from 'svelte/store';

  export type BackgroundTaskSetStatusFunction = (
    message?: string | null,
    progressPercentage?: number | null
  ) => void;

  export type BackgroundTaskCallback<T> = (
    client: BackgroundTaskClient<T>,
    setStatus: BackgroundTaskSetStatusFunction
  ) => Promise<T> | T;

  export interface BackgroundTask<T> {
    name: string;
    message: string | null;
    progress: number | null;
    retryable: boolean;
    status: BackgroundTaskStatus;
    cancelled: boolean;
    autoDismiss: boolean;
    hidden: boolean;
    lastUpdated: number;

    isDismissed: boolean;

    client: BackgroundTaskClient<any>;
    run: () => Promise<T>;
    dismiss: () => void;
    cancel: () => void;
  }

  export interface BackgroundTaskClient<T> {
    name: string;
    status: BackgroundTaskStatus;
    retryable: boolean;
    cancelled: boolean;
    run: () => Promise<T>;
    dismiss: () => void;
    task: Promise<T> | null;
    cancel: () => void;
  }

  export enum BackgroundTaskStatus {
    Ready,
    Running,
    Failed,
    Done
  }

  export const backgroundTasks: Writable<BackgroundTask<any>[]> = writable([]);
  export const runningBackgroundTasks: Readable<BackgroundTask<any>[]> = derived(
    backgroundTasks,
    (value) =>
      value.filter(
        (value) =>
          value.status == BackgroundTaskStatus.Running || value.status == BackgroundTaskStatus.Ready
      )
  );

  export const failedBackgroundTasks: Readable<BackgroundTask<any>[]> = derived(
    backgroundTasks,
    (value) => value.filter((value) => value.status == BackgroundTaskStatus.Failed)
  );

  export const completedBackgroundTasks: Readable<BackgroundTask<any>[]> = derived(
    backgroundTasks,
    (value) => value.filter((value) => value.status == BackgroundTaskStatus.Done)
  );

  export function dismissAll() {
    backgroundTasks.update((value) => {
      return value.filter((value) => value.status === BackgroundTaskStatus.Running);
    });
  }

  export function executeBackgroundTaskSeries<T>(
    name: string,
    retryable: boolean,
    callbacks: Array<BackgroundTaskCallback<T>>,
    autoDismiss: boolean = false
  ): BackgroundTaskClient<T[]> {
    const client = internalExecuteBackgroundTask<T[]>(
      name,
      retryable,
      async (): Promise<T[]> => {
        const toExec: Array<() => Promise<void>> = [];
        const promises: Promise<T>[] = callbacks.map((callback) =>
          (async () => {
            const client = executeBackgroundTask<T>(
              name,
              retryable,
              async (client, setStatus) => {
                setStatus('Waiting...', null);

                const { promise } = await new Promise<{ promise: Promise<T> }>((resolve) => {
                  toExec.push(async () => {
                    const promise = (async () => {
                      return await callback(client, setStatus);
                    })();

                    resolve({ promise });
                    await promise;
                  });
                });

                return await promise;
              },
              autoDismiss
            );

            return await client.run();
          })()
        );

        for (const exec of toExec) {
          await exec();
        }

        return await Promise.all(promises);
      },
      true,
      true
    );

    return client;
  }

  export function executeBackgroundTask<T>(
    name: string,
    retryable: boolean,
    callback: BackgroundTaskCallback<T>,
    autoDismiss: boolean = false
  ) {
    return internalExecuteBackgroundTask(name, retryable, callback, autoDismiss, false);
  }

  function internalExecuteBackgroundTask<T>(
    name: string,
    retryable: boolean,
    callback: BackgroundTaskCallback<T>,
    autoDismiss: boolean = false,
    hidden: boolean = false
  ): BackgroundTaskClient<T> {
    let firstRun: boolean = false;
    let task: Promise<T> | null = null;
    let client: BackgroundTaskClient<T>;

    const backgroundTask: BackgroundTask<T> = {
      name,
      message: null,
      progress: null,
      retryable,
      status: BackgroundTaskStatus.Ready,
      hidden,
      autoDismiss,
      lastUpdated: Date.now(),

      get isDismissed() {
        return get(backgroundTasks).indexOf(backgroundTask) === -1;
      },

      get client() {
        return client;
      },
      get run() {
        const refresh = () => {
          backgroundTask.lastUpdated = Date.now();
          backgroundTasks.update((e) => e);
        };
        const setStatus: BackgroundTaskSetStatusFunction = (
          message = backgroundTask.message,
          progress = backgroundTask.progress
        ) => {
          backgroundTask.message = message;
          backgroundTask.progress = progress;
          refresh();
        };

        const run = async (client: BackgroundTaskClient<T>): Promise<T> => {
          if (backgroundTask.status === BackgroundTaskStatus.Running) {
            throw new Error('Operation is already running.');
          }

          if (firstRun && !backgroundTask.retryable) {
            throw new Error('Operation cannot be retried.');
          }

          try {
            backgroundTask.cancelled = false;

            backgroundTask.status = BackgroundTaskStatus.Running;
            refresh();
            const result = await callback(client, setStatus);
            backgroundTask.status = BackgroundTaskStatus.Done;
            refresh();

            if (backgroundTask.autoDismiss) {
              backgroundTask.dismiss();
            }

            return result;
          } catch (error: any) {
            backgroundTask.status = BackgroundTaskStatus.Failed;
            setStatus(error?.message ?? 'An error occured.', null);
            throw error;
          }
        };

        return async () => {
          try {
            return await (task = run(client));
          } finally {
            task = null;
          }
        };
      },

      cancel: () => (backgroundTask.cancelled = true),
      dismiss: () => {
        if (backgroundTask.status == BackgroundTaskStatus.Running) {
          return;
        }

        backgroundTasks.update((value) => {
          const index = value.indexOf(backgroundTask);

          if (index >= 0) {
            value.splice(index, 1);
          }

          return value;
        });
      },

      cancelled: false
    };

    backgroundTasks.update((value) => {
      value.unshift(backgroundTask);

      return value;
    });

    client = {
      get cancelled() {
        return backgroundTask.cancelled;
      },
      get run() {
        return backgroundTask.run;
      },
      get cancel() {
        return backgroundTask.cancel;
      },
      get name() {
        return backgroundTask.name;
      },
      get retryable() {
        return backgroundTask.retryable;
      },
      get status() {
        return backgroundTask.status;
      },
      get task() {
        return task;
      },
      get dismiss() {
        return backgroundTask.dismiss;
      }
    };

    return client;
  }

  authentication.subscribe(() => {
    for (const task of get(backgroundTasks)) {
      task.cancel();
    }

    backgroundTasks.set([]);
  });
</script>

<script lang="ts">
  import { RefreshCwIcon, XIcon, PlayIcon } from 'svelte-feather-icons';
  import { onDestroy, onMount } from 'svelte';
  import { LoadingBar } from '@rizzzi/svelte-commons';

  export let maxCount: number = -1;

  export let filter: (list: BackgroundTask<any>[]) => BackgroundTask<any>[] = (value) => value;

  let cached: BackgroundTask<any>[] = [];

  let unsubscriber: () => void;

  onMount(() => {
    let preCached: BackgroundTask<any>[] | null = [];
    let running: boolean = false;

    let lastTime: number = 0;

    unsubscriber = backgroundTasks.subscribe((value) => {
      preCached = filter(value);

      if (!running) {
        running = true;

        const update = () => {
          if (preCached == null) {
            running = false;
            return;
          }

          cached = preCached.slice(0, maxCount < 0 ? preCached.length : maxCount);
          preCached = null;
          lastTime = Date.now();

          requestAnimationFrame(update);
        };

        requestAnimationFrame(update);
      }
    });
  });

  onDestroy(() => {
    unsubscriber();
  });
</script>

<div class="background-tasks">
  {#if cached.length == 0}
    <p>No pending operations.</p>
  {:else}
    {#each cached.toSorted((a, b) => a.status - b.status) as { name, progress, run, retryable, cancelled, cancel, message, status, dismiss }, index}
      {#if index != 0}
        <div class="divider"></div>
      {/if}

      <div class="background-task">
        <div class="name">
          <p><b>{name}</b></p>

          {#if status == BackgroundTaskStatus.Running}
            {#if !cancelled}
              <button on:click={() => cancel()} title="Cancel">
                <XIcon size="16" />
              </button>
            {/if}
          {:else if status == BackgroundTaskStatus.Done}
            <button on:click={() => dismiss()} title="Dismiss">
              <XIcon size="16" />
            </button>
          {:else if status == BackgroundTaskStatus.Failed}
            {#if retryable}
              <button on:click={() => run()} title="Run">
                <RefreshCwIcon size="16" />
              </button>
            {/if}
            <button on:click={() => dismiss()} title="Dismiss">
              <XIcon size="16" />
            </button>
          {:else if status == BackgroundTaskStatus.Ready}
            <button on:click={() => run()} title="Run">
              <PlayIcon size="16" />
            </button>
            <button on:click={() => dismiss()} title="Dismiss">
              <XIcon size="16" />
            </button>
          {/if}
        </div>
        {#if status == BackgroundTaskStatus.Running}
          <div class="progress">
            <LoadingBar bind:progress />
          </div>
        {/if}
        <div class="message">
          {#if status == BackgroundTaskStatus.Failed}
            <p>Failed: {message}</p>
          {:else}
            {#if message != null}
              <p>{message}</p>
            {/if}
            {#if progress != null}
              <p>{Math.round(progress * 1000) / 10}%</p>
            {/if}
          {/if}
        </div>
      </div>
    {/each}
  {/if}
</div>

<style lang="scss">
  div.background-tasks {
    display: flex;
    flex-direction: column;

    // min-height: 64px;
    overflow-y: auto;

    padding: 8px 0px 8px 0px;

    gap: 8px;

    > p {
      text-align: center;
    }

    > div.divider {
      min-height: 1px;
      max-height: 1px;

      background-color: var(--onPrimary);
    }

    > div.background-task {
      font-size: 10px;
      display: flex;
      flex-direction: column;

      gap: 8px;

      > div.name {
        display: flex;
        flex-direction: row;
        align-items: center;

        gap: 9px;

        > p {
          margin: 0px;

          font-size: 12px;
        }

        > p:nth-child(1) {
          flex-grow: 1;
          min-width: 0px;
        }

        > button {
          background-color: unset;
          border: unset;
          color: var(--onPrimary);

          cursor: pointer;

          padding: 0px;

          width: 16px;
          height: 16px;
        }
      }

      > div.message {
        display: flex;
        flex-direction: row;
        align-items: center;

        > p {
          margin: 0px;
        }

        > p:nth-child(1) {
          flex-grow: 1;
          min-width: 0px;
        }

        > p:nth-child(2) {
          max-width: 100%;
        }
      }
    }
  }
</style>

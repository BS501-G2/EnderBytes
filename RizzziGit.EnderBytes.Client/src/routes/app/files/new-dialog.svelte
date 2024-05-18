<script lang="ts" context="module">
  export interface NewDialogState {
    x: number;
    y: number;

    state:
      | {
          type: 'file';
          files: File[];
        }
      | {
          type: 'folder';
          name: string;
        };
  }

  export const newDialogState: Writable<NewDialogState | null> = writable(null);
</script>

<script lang="ts">
  import {
    Button,
    ButtonClass,
    Dialog,
    Input,
    LoadingSpinner,
    Overlay,
    OverlayPositionType,
    ResponsiveLayout
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { fly } from 'svelte/transition';

  const { onNew }: { onNew: (id: number) => void } = $props();

  function onDismiss() {
    $newDialogState = null;
  }

  const tabs: [type: 'file' | 'folder', name: string, icon: string, description: string][] = [
    ['file', 'File', 'fa-solid fa-file', 'Create a new file.'],
    ['folder', 'Folder', 'fa-solid fa-folder', 'Create a new folder.']
  ];
</script>

{#if $newDialogState != null}
  {@const { x, y } = $newDialogState}

  <ResponsiveLayout>
    {#snippet desktop()}
      <Overlay position={[OverlayPositionType.Offset, x - 2, y + 8]} {onDismiss}>
        <div class="new-overlay" transition:fly|global={{ duration: 200, y: -16 }}>
          {@render content($newDialogState!)}
        </div>
      </Overlay>
    {/snippet}

    {#snippet mobile()}
      <Dialog {onDismiss}>
        {#snippet head()}
          <h2>New</h2>
        {/snippet}
        {#snippet body()}
          {@render content($newDialogState!)}
        {/snippet}
      </Dialog>
    {/snippet}
  </ResponsiveLayout>
{/if}

{#snippet content({ state }: NewDialogState)}
  <div class="new-container">
    <div class="tab-host">
      {#each tabs as [type, name, icon, description]}
        <Button
          buttonClass={ButtonClass.Transparent}
          enabled={state.type != type}
          onClick={() => {
            if ($newDialogState === null) {
              return;
            }

            if (type == 'file') {
              $newDialogState.state = { type: 'file', files: [] };
            } else if (type == 'folder') {
              $newDialogState.state = { type: 'folder', name: '' };
            }

            $newDialogState = $newDialogState;
          }}
          outline={false}
          hint={description}
        >
          <div class="button {state.type == type ? 'active' : ''}">
            <i class={icon}></i>
            <p>{name}</p>
          </div>
        </Button>
      {/each}
    </div>
    <div class="tab-view">
      {#if state.type == 'folder'}
        <div class="input-row">
          <Input type="text" name="Folder Name" bind:text={state.name} />
          <Button
            buttonClass={ButtonClass.Primary}
            onClick={async () => {
          await new Promise<void>((resolve) => setTimeout(resolve, 1000))
          onDismiss()
        }}
          >
            <p class="button">Create Folder</p>
            {#snippet loading()}
              <p class="button"><LoadingSpinner size="1em" /></p>
            {/snippet}
          </Button>
        </div>
      {/if}
    </div>
  </div>
{/snippet}

<style lang="scss">
  div.new-container {
    display: flex;
    flex-direction: column;
    gap: 8px;

    > div.tab-view {
      padding: 8px;

      div.input-row {
        display: flex;
        flex-direction: column;
        align-items: stretch;
        gap: 8px;
      }

      p.button {
        margin: 8px;
      }
    }

    > div.tab-host {
      display: flex;
      flex-direction: row;
      gap: 8px;

      div.button {
        display: flex;
        flex-direction: row;
        align-items: center;

        gap: 8px;
        padding: 8px;
        border: 1px solid transparent;
      }

      div.button.active {
        border-bottom-color: var(--primary);
      }
    }
  }

  div.new-overlay {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    padding: 8px;
    border-radius: 8px;

    display: flex;
    flex-direction: column;

    box-shadow: var(--shadow) 0px 0px 8px;
  }
</style>

<script lang="ts">
  import {
    Awaiter,
    Tab,
    Input,
    Dialog,
    Button,
    type TabItem,
    createTabId,
    ButtonClass
  } from '@rizzzi/svelte-commons';
  import { type Writable, writable } from 'svelte/store';

  import { onMount } from 'svelte';
  import { clientSideInvoke } from '$lib/client/api';

  const enabled: Writable<boolean> = writable(false);

  onMount(async () => {
    $enabled = true;
  });

  const tabs: TabItem[] = [
    {
      name: 'Login Credentials',
      view: loginCredentialsTab
    },
    {
      name: 'Profile',
      view: profileTab
    }
  ];

  const tabId = createTabId(tabs);

  const username: Writable<string> = writable('');
  const password: Writable<string> = writable('');
  const confirmPassword: Writable<string> = writable('');
  const firstName: Writable<string> = writable('');
  const middleName: Writable<string> = writable('');
  const lastName: Writable<string> = writable('');
</script>

{#snippet loginCredentialsTab()}
  <p>Put in your login credentials</p>

  <div class="input-row">
    <Input bind:text={$username} type="text" name="Username" required={true} />
  </div>
  <div class="input-row">
    <Input bind:text={$password} type="password" name="Password" required={true} />
    <Input bind:text={$confirmPassword} type="password" name="Confirm Password" required={true} />
  </div>
{/snippet}

{#snippet profileTab()}
  <p>Put in your profile information</p>

  <div class="input-row">
    <Input bind:text={$firstName} type="text" name="First Name" required={true} />
  </div>

  <div class="input-row">
    <Input bind:text={$middleName} type="text" name="Middle Name" />
  </div>

  <div class="input-row">
    <Input bind:text={$lastName} type="text" name="Last Name" required={true} />
  </div>
{/snippet}

{#if $enabled}
  <Dialog
    onDismiss={() => {
      $enabled = false;
      // goto('/', { replaceState: false });

      setTimeout(() => {
        location.reload();
      }, 100);
    }}
  >
    {#snippet head()}
      <h2>Administrator Account Setup</h2>
    {/snippet}

    {#snippet body()}
      <Tab id={tabId}>
        {#snippet view(view)}
          <div class="tab-view">
            {@render view()}
          </div>
        {/snippet}
      </Tab>
    {/snippet}

    {#snippet actions()}
      <Tab id={tabId}>
        {#snippet view()}{/snippet}
        {#snippet host(tabs, currentTabIndex, setTab)}
          <Button
            onClick={() => setTab(currentTabIndex - 1)}
            buttonClass={ButtonClass.Background}
            enabled={currentTabIndex > 0}
          >
            <div class="button">Previous</div>
          </Button>

          <Button
            onClick={async () => {
              if (currentTabIndex !== tabs.length - 1) {
                setTab(currentTabIndex + 1);
              } else {
                await clientSideInvoke('createAdminUser', {
                  username: $username,
                  password: $password,
                  firstName: $firstName,
                  middleName: $middleName,
                  lastName: $lastName
                });
              }
            }}
          >
            <div class="button">
              {#if currentTabIndex !== tabs.length - 1}
                Next
              {:else}
                Submit
              {/if}
            </div>
          </Button>
        {/snippet}
      </Tab>
    {/snippet}
  </Dialog>
{/if}

<style lang="scss">
  div.button {
    padding: 8px;
  }

  div.tab-view {
    min-width: min(100vw, 512px);

    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  div.input-row {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 8px;

    > :global(*) {
      flex-grow: 1;
    }
  }
</style>

<script lang="ts" context="module">
  import { writable, type Writable } from 'svelte/store';

  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import { Dialog, Expandable } from '@rizzzi/svelte-commons';
</script>

{#if $enabled}
  <Dialog onDismiss={() => ($enabled = false)}>
    {#snippet head()}
      <h2 style="margin-left: 20px; margin-top:20px;">Basic Info</h2>
    {/snippet}
    {#snippet body()}
      <div class="account-settings" style="height: 700px; width:800px; overflow: hidden;">
        <h2 style="margin-left: 20px; margin-top:20px;">Contact Info</h2>

        <div>
          <hr
            style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"
          />
        </div>

        <div class="Email" style="margin-top:20px; margin-left:20px; margin-right:20px;">
          <Expandable>
            <svelte:fragment slot="header" let:toggle>
              <div style="display:flex; justify-content:space-between;">
                <p style="font-weight: 500; font-size:large; color:black">Email</p>
                <p style="font-weight: 300; font-size:large; color:black">
                  UserTest@EnderDrive.com
                </p>
                <button
                  on:click={toggle}
                  style="background-color: transparent; border:none; cursor:pointer;"
                >
                  <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
                </button>
              </div>
            </svelte:fragment>
            <svelte:fragment slot="body">
              <div>
                <div style="display:flex; justify-content:center">
                  <div style="position: relative; width:40%;">
                    <input
                      type="email"
                      id="emailInput"
                      style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;"
                      required
                    />
                    <span
                      style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;"
                      >Email</span
                    >
                  </div>
                </div>
                <div
                  style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px"
                >
                  <button
                    style="
        background-color: var(--primaryContainer);
        cursor: pointer;
        border-color: var(--onPrimaryContainerVariant);
        color: white;
        padding: 10px 23px;
        text-align: center;
        text-decoration: none;
        display: inline-block;
        font-size: 16px;
        border-radius:10px;
        font-weight: bold;
        ">Apply</button
                  >
                </div>
              </div>
            </svelte:fragment>
          </Expandable>
        </div>

        <div>
          <hr
            style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"
          />
        </div>

        <div class="Phone" style="margin-top:20px; margin-left:20px; margin-right:20px;">
          <Expandable>
            <svelte:fragment slot="header" let:toggle>
              <div style="display:flex; justify-content:space-between;">
                <p style="font-weight: 500; font-size:large; color:black">Phone</p>
                <p style="font-weight: 300; font-size:large; color:black">+63</p>
                <button
                  on:click={toggle}
                  style="background-color: transparent; border:none; cursor:pointer;"
                >
                  <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
                </button>
              </div>
            </svelte:fragment>
            <svelte:fragment slot="body">
              <div>
                <div style="display:flex; justify-content:center">
                  <div style="position: relative; width:40%;">
                    <input
                      type="tel"
                      id="phoneInput"
                      style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;"
                      required
                      pattern="[0-9]+"
                    />
                    <span
                      style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;"
                      >Phone</span
                    >
                  </div>
                </div>
                <div
                  style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px"
                >
                  <button
                    style="
          background-color: var(--primaryContainer);
          cursor: pointer;
          border-color: var(--onPrimaryContainerVariant);
          color: white;
          padding: 10px 23px;
          text-align: center;
          text-decoration: none;
          display: inline-block;
          font-size: 16px;
          border-radius:10px;
          font-weight: bold;
          ">Apply</button
                  >
                </div>
              </div>
            </svelte:fragment>
          </Expandable>
        </div>

        <h2 style="margin-left: 20px; margin-top:20px;">Security & Password</h2>

        <div>
          <hr
            style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"
          />
        </div>

        <div class="Email" style="margin-top:20px; margin-left:20px; margin-right:20px;">
          <Expandable>
            <svelte:fragment slot="header" let:toggle>
              <div style="display:flex; justify-content:space-between;">
                <p style="font-weight: 500; font-size:large; color:black">Language</p>
                <p style="font-weight: 300; font-size:large; color:black">Select Language</p>
                <button
                  on:click={toggle}
                  style="background-color: transparent; border:none; cursor:pointer;"
                >
                  <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
                </button>
              </div>
            </svelte:fragment>
            <svelte:fragment slot="body">
              <div>
                <div style="display: flex; justify-content: center; align-items: center;">
                  <div style="width: 25px; height: 50px; color: white; border: none;">
                    <select class="language-select">
                      <option value="en">English</option>
                      <option value="fil">Filipino</option>
                    </select>
                  </div>
                </div>
                <div
                  style="display: flex; justify-content: center; align-items: center; height: 50px; width: 800px;"
                >
                  <button
                    style="
                background-color: var(--primaryContainer);
                cursor: pointer;
                border-color: var(--onPrimaryContainerVariant);
                color: white;
                padding: 10px 20px;
                text-align: center;
                text-decoration: none;
                display: flex;
                justify-content: center;
                font-size: 16px;
                border-radius:10px;
                font-weight: bold;
                ">Save</button
                  >
                </div>
              </div>
            </svelte:fragment>
          </Expandable>
        </div>

        <div>
          <hr
            style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"
          />
        </div>

        <div class="Password" style="margin-top:20px; margin-left:20px; margin-right:20px;">
          <Expandable>
            <svelte:fragment slot="header" let:toggle>
              <div style="display:flex; justify-content:space-between;">
                <p style="font-weight: 500; font-size:large; color:black">Password</p>
                <p style="font-weight: 300; font-size:large; color:black">Change Password</p>
                <button
                  on:click={toggle}
                  style="background-color: transparent; border:none; cursor:pointer;"
                >
                  <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
                </button>
              </div>
            </svelte:fragment>
            <svelte:fragment slot="body">
              <div>
                <div style="display:flex; justify-content:center">
                  <div style="position: relative; width:20%;">
                    <input
                      type="Pass"
                      id="current-password"
                      style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;"
                    />
                    <span
                      style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;"
                      >Current Password</span
                    >
                  </div>
                  <div style="display:flex; justify-content:center">
                    <div style="position: relative; width:80%; ">
                      <input
                        type="Pass"
                        id="current-password"
                        style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;"
                      />
                      <span
                        style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;"
                        >New Password</span
                      >
                    </div>
                  </div>

                  <div
                    style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px;"
                  >
                    <button
                      style="
                      background-color: var(--primaryContainer);
                      cursor: pointer;
                      border-color: var(--onPrimaryContainerVariant);
                      color: white;
                      padding: 10px 23px;
                      text-align: center;
                      text-decoration: none;
                      display: inline-block;
                      font-size: 16px;
                      border-radius:10px;
                      font-weight: bold;
                      left: 50px;
                      ">Save & Change</button
                    >
                  </div>
                </div>
              </div></svelte:fragment
            >
          </Expandable>
          <div class="buttonDiscardSave" style="Display:flex; margin-top:125px; margin-left:575px">
            <button
              style="
          background-color: var(--error);
          cursor: pointer;
          border-color: var(--onPrimaryContainerVariant);
          color: white;
          padding: 10px 15px;
          text-align: center;
          text-decoration: none;
          display: inline-block;
          font-size: 16px;
          border-radius:10px;
          font-weight: bold;
          margin-right:20px;
          ">Discard</button
            >
            <button
              style="
          background-color: var(--primaryContainer);
          cursor: pointer;
          border-color: var(--onPrimaryContainerVariant);
          color: white;
          padding: 10px 23px;
          text-align: center;
          text-decoration: none;
          display: inline-block;
          font-size: 16px;
          border-radius:10px;
          font-weight: bold;
          ">Save</button
            >
          </div>
        </div>
      </div>
    {/snippet}
  </Dialog>
{/if}

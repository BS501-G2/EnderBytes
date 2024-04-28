<script lang="ts" context="module">
  import { writable, type Writable } from "svelte/store";

  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import Dialog from "../Widgets/Dialog.svelte";

  import Expandable from "../Widgets/Expandable.svelte";

  // export let targetElement: HTMLElement | null = null;
</script>

{#if $enabled}
  <Dialog onDismiss={() => $enabled = false}>
    <h2 slot="head" style="margin-left: 20px; margin-top:20px;">Basic Info</h2>

    <div class="account-settings" slot="body" style="height: 700px; width:800px; overflow: hidden;">

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

        <div class="profilePicture" style="margin-top:20px; margin-left:20px; margin-right:20px;">
        <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Profile Picture</p>
              <p style="font-weight: 300; font-size:large; color:black">Add a profile picture to personalize your account</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="far fa-image" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <p>Expanded</p>
          </fragment>
        </Expandable>
      </div>

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

      <div class="Name" style="margin-top:20px; margin-left:20px; margin-right:20px;">
       <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Name</p>
              <p style="font-weight: 300; font-size:large; color:black">First name, Middle name, Last name</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <div>
              <div style="display:flex; justify-content:space-between">
                <div style="position: relative;">
                  <input type="text" id="FnameInput" style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;" required>
                  <span style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;">First name</span>
                </div>
                <div style="position: relative;">
                  <input type="text" id="MnameInput" style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;" required>
                  <span style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;">Middle name</span>
                </div>
                <div style="position: relative;">
                  <input type="text" id="LnameInput" style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;" required>
                  <span style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;">Last name</span>
                </div>
                
              </div>
              <div style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px">
                <button style="
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
                ">Apply</button>
              </div>
              
            </div>
          </fragment>
        </Expandable>

      </div>

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

      <div class="Birthday" style="margin-top:20px; margin-left:20px; margin-right:20px;">
        <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Birthday</p>
              <p style="font-weight: 300; font-size:large; color:black">Month, Day, Year</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <div>
              <div style="display:flex; justify-content:center">
                <div style="position: relative;">
                  <input type="date" id="dateInput" style="width: 100%; padding: 17px 30px; margin: 8px 0; box-sizing: border-box;" required>
                </div>
              </div>
              <div style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px">
                <button style="
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
                ">Apply</button>
              </div>
              
            </div>
          </fragment>
        </Expandable>

      </div>

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

      <div class="Gender" style="margin-top:20px; margin-left:20px; margin-right:20px;">
        <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Gender</p>
              <p style="font-weight: 300; font-size:large; color:black">Male/ Female/ Others</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <div>
              <div style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px;">
                <div style="position: relative; margin-right: 20px;">
                  <input type="radio" id="male" name="gender" style="margin-right: 5px;">
                  <label for="male">Male</label>
                </div>
                <div style="position: relative; margin-right: 20px;">
                  <input type="radio" id="female" name="gender" style="margin-right: 5px;">
                  <label for="female">Female</label>
                </div>
                <div style="position: relative;">
                  <input type="radio" id="other" name="gender" style="margin-right: 5px;">
                  <label for="other">Other</label>
                </div>
              </div>
              <div style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px">
                <button style="
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
                  ">Apply</button>
              </div>
            </div>
            
          </fragment>
        </Expandable>


      </div>
      <h2 style="margin-left: 20px; margin-top:20px;">Contact Info</h2>

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

      <div class="Email" style="margin-top:20px; margin-left:20px; margin-right:20px;">
        <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Email</p>
              <p style="font-weight: 300; font-size:large; color:black">UserTest@EnderDrive.com</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <div>
              <div style="display:flex; justify-content:center">
                <div style="position: relative; width:40%;">
                  <input type="email" id="emailInput" style="width: 100%; padding: 17px 20px; margin: 8px 0; box-sizing: border-box;" required>
                  <span style="position: absolute; top: 0; left: 0; padding: 10px; color: gray; font-size: 12px;">Email</span>
                </div>
              </div>
              <div style="display:flex; justify-content:center; padding-top:10px; padding-bottom:10px">
                <button style="
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
                ">Apply</button>
              </div>
              
            </div>
          </fragment>
        </Expandable>
      </div>

      <div><hr style="width: 100%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"></div>

      <div class="Phone" style="margin-top:20px; margin-left:20px; margin-right:20px;">
        <Expandable>
          <fragment slot='header' let:toggle>
            <div style="display:flex; justify-content:space-between;">
              <p style="font-weight: 500; font-size:large; color:black">Phone</p>
              <p style="font-weight: 300; font-size:large; color:black">09291293299</p>
              <button on:click={toggle} style="background-color: transparent; border:none; cursor:pointer;">
              <i class="fas fa-chevron-right" style="font-size:40px; color:black"></i>
              </button>
            </div>
          </fragment>
          <fragment slot='body'>
            <p>Expanded</p>
          </fragment>
        </Expandable>

      </div>
      <div class="buttonDiscardSave" style="Display:flex; margin-top:125px; margin-left:575px">
        <button style="
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
        ">Discard</button>
        <button style="
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
        ">Save</button>
      </div>


    </div>
  </Dialog>
{/if}

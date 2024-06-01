<script lang="ts" context="module">
  export enum UserResolveType {
    Username,
    UserId
  }
  export type UserResolve =
    | { type: UserResolveType.UserId; userId: number }
    | { type: UserResolveType.Username; username: string };
</script>

<script lang="ts">
  import { Title, Awaiter, Button, ButtonClass } from '@rizzzi/svelte-commons';
  import { showSettingsDialog } from '../settings-dialog.svelte';
  import { getUser } from '$lib/client/api-functions';

  export let identifier: UserResolve;
  let userPromise: Promise<any> | null;

  async function resolve(): Promise<any | null> {
    if (identifier.type == UserResolveType.Username) {
      return await getUser(identifier.username);
    } else {
      return await getUser(identifier.userId);
    }
  }
</script>

<svelte:head>
  {#if identifier.type == UserResolveType.UserId}
    {#key userPromise}
      <Awaiter callback={() => userPromise}>
        {#snippet success({ result: user })}
          <link rel="canonical" href="@{user.Username}" />
        {/snippet}
      </Awaiter>
    {/key}
  {/if}
</svelte:head>

<div class="user-page">
  <Awaiter callback={() => (userPromise = resolve())}>
    {#snippet success({ result: user })}
      <Title
        title="{user.firstName}{user.middleName != null
          ? ` ${user.middleName}.`
          : ''} {user.lastName} (@{user.username})"
      />

      <div>
        <div
          class="top w-50"
          style="height:450px; background-color: var(--backgroundVariant); border-radius: 15px; margin:20px;"
        >
          <div
            class="EditProfile"
            style="position: absolute; top: 70px; right: 0; margin: 20px; margin-right: 50px;"
          >
            <Button
              buttonClass={ButtonClass.PrimaryContainer}
              onClick={() => {
                showSettingsDialog();
              }}>Edit Profile</Button
            >
          </div>

          <div class="container" style="text-align: center;">
            <div class="picture" style="padding-top: 25px;">
              <img
                src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                class="mx-auto d-block"
                alt="..."
                style="border-radius: 160px; height: 200px; width: 200px; display: block; margin: auto;"
              />
            </div>
            <div class="fullname" style="padding-top:15px;">
              <div>
                {user.lastName}, {user.firstName}
                {user.middleName ? `${user.middleName[0]}.` : ''}
              </div>
              <div class="username" style="padding-top:5px">(@{user.username})</div>
              <hr
                style="width: 90%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant); margin: 20px auto;"
              />

              <div style="display:flex; justify-content:center;">
                <div class="photos">
                  <button
                    style="border:none;
                cursor:pointer;
                background-color: transparent;"
                  >
                    <div style="display:flex;">
                      <p
                        style="border-color: var(--onPrimaryContainerVariant);
                  background-color: var(--onPrimaryContainerVariant);
                  padding:5px; 5px;
                  border-radius:8px;
                  font-weight: bold;
                  font-size:14px;
                  color: var(--onPrimaryVariant);"
                      >
                        14
                      </p>
                      <p
                        style="font-weight: bold;
                  padding:5px;5px;
                  font-size:14px;"
                      >
                        Photos
                      </p>
                    </div>
                  </button>
                </div>

                <div class="videos">
                  <button
                    style="border:none;
                cursor:pointer;
                background-color: transparent;"
                  >
                    <div style="display:flex;">
                      <p
                        style="border-color: var(--onPrimaryContainerVariant);
                  background-color: var(--onPrimaryContainerVariant);
                  padding:5px; 5px;
                  border-radius:8px;
                  font-weight: bold;
                  font-size:14px;
                  color: var(--onPrimaryVariant);"
                      >
                        26
                      </p>
                      <p
                        style="font-weight: bold;
                  padding:5px;5px;
                  font-size:14px;"
                      >
                        Videos
                      </p>
                    </div>
                  </button>
                </div>

                <div class="docs">
                  <button
                    style="border:none;
                cursor:pointer;
                background-color: transparent;"
                  >
                    <div style="display:flex;">
                      <p
                        style="border-color: var(--onPrimaryContainerVariant);
                  background-color: var(--onPrimaryContainerVariant);
                  padding:5px; 5px;
                  border-radius:8px;
                  font-weight: bold;
                  font-size:14px;
                  color: var(--onPrimaryVariant);"
                      >
                        120
                      </p>
                      <p
                        style="font-weight: bold;
                  padding:5px;5px;
                  font-size:14px;"
                      >
                        Docs
                      </p>
                    </div>
                  </button>
                </div>

                <div class="Audios">
                  <button
                    style="border:none;
                cursor:pointer;
                background-color: transparent;"
                  >
                    <div style="display:flex;">
                      <p
                        style="border-color: var(--onPrimaryContainerVariant);
                  background-color: var(--onPrimaryContainerVariant);
                  padding:5px; 5px;
                  border-radius:8px;
                  font-weight: bold;
                  font-size:14px;
                  color: var(--onPrimaryVariant);"
                      >
                        3
                      </p>
                      <p
                        style="font-weight: bold;
                  padding:5px;5px;
                  font-size:14px;"
                      >
                        Audios
                      </p>
                    </div>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div
          class="bottom w-50"
          style="height:450px; background-color: var(--onPrimary); border-radius: 15px; margin:20px;"
        >
          <div
            class="botTitle"
            style="padding-top: 30px;display: flex; justify-content: space-between;"
          >
            <p style="font-weight: bold; font-size: large;padding-left: 80px;">
              Recent Shared Files
            </p>
            <a href="#a" style="padding-right: 60px;">See All</a>
          </div>
          <div>
            <hr
              style="width: 90%; border: none; border-top: 1px solid var(--onPrimaryContainerVariant);"
            />
          </div>

          <div class="file-container" style="display:flex;">
            <div class="file1" style="position: relative; width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:80px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">Arvin</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Today at 8:08 PM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 50px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Vlcsnap-2024-04-14xcdarvincute.xcf</a>
                    </p>
                    <p style="margin: 0; font-size: small;">3.14 MB</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="file2" style="position: relative;width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:70px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">John</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Yesterday at 3:08 AM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 45px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Wiwo-2024-04-13xcdarvincute.jpg</a>
                    </p>
                    <p style="margin: 0; font-size: small;">4.14 GB</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="file3" style="position: relative; width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:80px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">Arvin</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Today at 8:08 PM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 50px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Vlcsnap-2024-04-14xcdarvincute.xcf</a>
                    </p>
                    <p style="margin: 0; font-size: small;">3.14 MB</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="file-container" style="display:flex;">
            <div class="file1" style="position: relative; width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:80px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">Arvin</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Today at 8:08 PM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 50px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Vlcsnap-2024-04-14xcdarvincute.xcf</a>
                    </p>
                    <p style="margin: 0; font-size: small;">3.14 MB</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="file2" style="position: relative;width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:70px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">John</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Yesterday at 3:08 AM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 45px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Wiwo-2024-04-13xcdarvincute.jpg</a>
                    </p>
                    <p style="margin: 0; font-size: small;">4.14 GB</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="file3" style="position: relative; width:40%; ">
              <div
                class="picture"
                style="display: flex; align-items: center; margin-left:80px; margin-top:10px"
              >
                <img
                  src="https://img.freepik.com/free-vector/cute-hamster-holding-cheek-cartoon-illustration_138676-2773.jpg?size=338&ext=jpg&ga=GA1.1.1700460183.1712793600&semt=sph"
                  class="mx-auto d-block"
                  alt="..."
                  style="border-radius: 160px; height: 50px; width: 50px; margin-right: 10px;"
                />
                <div style="margin-bottom: 1px;">
                  <p style="margin: 0; font-weight: bold; display:inline-block">Arvin</p>
                  <p
                    style="margin: 0; font-weight: lighter; font-size: small; display:inline-block"
                  >
                    Today at 8:08 PM
                  </p>
                </div>
              </div>
              <div class="filebox-container" style="position: relative; margin-left: 50px;">
                <div style="position: absolute; top: -10px; left: 330px;">
                  <button
                    class="downloadbutton"
                    style="background-color: none; color: black; border: 1px solid var(--onPrimaryContainerVariant); padding: 15px 17px; border-radius: 5px; cursor: pointer;"
                  >
                    <i class="fas fa-download" style="font-size: 18px;"></i>
                  </button>
                </div>
                <div
                  class="filebox"
                  style="border-radius: 25px; background: var(--background); padding: 20px; width: 250px; height: auto; margin-left: 70px; display: flex; align-items: center;margin-top: -10px;"
                >
                  <img
                    src="https://www.iconpacks.net/icons/2/free-file-icon-1453-thumb.png"
                    alt="File Logo"
                    style="height: 50px; width: 50px; margin-right: 15px;"
                  />
                  <div style="flex-grow: 1; max-width: calc(100% - 80px);">
                    <p
                      style="margin: 0; font-size: small; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"
                    >
                      <a href="file_url">Vlcsnap-2024-04-14xcdarvincute.xcf</a>
                    </p>
                    <p style="margin: 0; font-size: small;">3.14 MB</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    {/snippet}
  </Awaiter>
</div>

<style lang="scss">
  div.user-page {
    width: 100%;
    height: 100%;
  }
</style>

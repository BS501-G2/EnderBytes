<script lang="ts">
  import Awaiter from "../../components/Bindings/Awaiter.svelte";
import Client, { interpretResponse } from "../../components/Bindings/Client.svelte";

  let username: string;
  let password: string;
</script>

<Client let:fetch let:session>
  <div>
    Session: <pre>{JSON.stringify(session)}</pre>
  </div>

  <input type="username" bind:value={username} />
  <input type="password" bind:value={password} />

  <button
    on:click={() =>
      fetch("/auth/password-login", "POST", { username, password })}
  >
    Login
  </button>

  <Awaiter callback={async () => await interpretResponse(await fetch("/user/@testuser", "GET"))}>
    <svelte:fragment slot="success" let:result={user}>
    <pre>{JSON.stringify(user, undefined, '  ')}</pre>
    </svelte:fragment>
  </Awaiter>
</Client>

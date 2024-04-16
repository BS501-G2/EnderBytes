<script lang="ts">
  import Client from "../../components/Bindings/Client.svelte";

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
</Client>

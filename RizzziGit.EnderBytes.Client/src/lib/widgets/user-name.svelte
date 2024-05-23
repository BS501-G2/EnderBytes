<script lang="ts" context="module">
  export enum UserClass {
    Link
  }

  export type UserProps = {
    user: any;
  } & (
    | {
        class: UserClass.Link;
        initials: boolean;
      }
    | {
        class?: undefined;
      }
  );
</script>

<script lang="ts">
  const { ...props }: UserProps = $props();
</script>

{#if props.class == null}
  <svelte:self user={props.user} class={UserClass.Link} initials={false} />
{:else if props.class == UserClass.Link}
  <a href="/app/users?id=@{props.user.username}">
    {props.user.lastName}, {props.initials
      ? `${props.user.firstName[0]}.`
      : props.user.firstName}{props.user.middleName ? ` ${props.user.middleName[0]}.` : ''}
  </a>
{/if}

<style lang="scss">
  a {
    text-decoration: none;
    color: inherit;
  }

  a:hover {
    text-decoration: underline;
  }
</style>

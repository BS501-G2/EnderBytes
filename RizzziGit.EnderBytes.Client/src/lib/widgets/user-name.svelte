<script lang="ts">
	export let user: any;

	export let initialName: boolean = false;
</script>

{user.lastName}, {initialName ? `${user.firstName[0]}.` : user.firstName}{user.middleName
	? ` ${user.middleName[0]}.`
	: ''}

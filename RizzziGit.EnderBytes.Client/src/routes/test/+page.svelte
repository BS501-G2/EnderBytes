<script lang="ts">
	import BindThis from './BindThis.svelte';
	import Runes from './Runes.svelte';
	import Trail from './Trail.svelte';

	let number: number = 0
</script>

<Trail />
<BindThis />

<p>{number}</p>
<Runes bind:number={number}/>

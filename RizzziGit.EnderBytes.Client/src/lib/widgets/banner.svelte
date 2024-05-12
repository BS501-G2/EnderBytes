<script lang="ts" context="module">
	export enum BannerClass {
		Info = 'info',
		Warning = 'warning',
		Error = 'error'
	}
</script>

<script lang="ts">
	export let bannerClass: BannerClass = BannerClass.Info;
</script>

<div class="content">
	<div class="banner {bannerClass}">
		<slot />
	</div>
</div>

<style lang="scss">
	div.content {
		padding: 16px;

		> div.banner {
			padding: 16px;
			box-sizing: border-box;

			border-radius: 8px;

			color: var(--onError);
		}

		> div.banner.error {
			border: solid 1px var(--error);
			background-color: var(--errorBackground);
		}

		> div.banner.warning {
			border: solid 1px var(--warning);
			background-color: var(--warningBackground);
		}

		> div.banner.info {
			border: solid 1px var(--info);
			background-color: var(--infoBackground);
		}
	}
</style>

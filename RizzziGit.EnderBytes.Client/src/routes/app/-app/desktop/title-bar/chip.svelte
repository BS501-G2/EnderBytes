<div class="title-bar-chip">
  <slot />
</div>

<style lang="scss">
  div.title-bar-chip {
    -webkit-app-region: no-drag;

    // margin: 8px 0px 8px 0px;

    border-style: none;
    border-radius: 8px;

    color: var(--onPrimaryContainer);

    display: flex;

    // align-items: center;
    gap: 4px;

    background-color: var(--primary);
  }
</style>

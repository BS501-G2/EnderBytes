<div class="title-bar-chip">
  <slot/>
</div>

<style lang="scss">
  div.title-bar-chip {
    user-select: none;

    margin: 8px;
    padding: 8px;

    border-style: none;
    border-radius: 8px;

    color: var(--onPrimaryContainer);

    display: flex;

    align-items: center;
    gap: 4px;

    background-color: var(--primary);
  }
</style>

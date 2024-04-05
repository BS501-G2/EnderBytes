<script lang="ts">
</script>

<div class="file-area">
  <div class="file-list">

  </div>

  <div class="divider"></div>

  <div class="file-details">

  </div>
</div>

<style lang="scss">
  div.file-area {
    display: flex;

    flex-grow: 1;

    > div.file-list {
      flex-grow: 1;

      padding: 16px;  
    }

    > div.divider {
      width: 1px;
      background-color: var(--primaryContainer);
    }

    > div.file-details {
      max-width: 320px;
      flex-grow: 1;

      padding: 16px;
    }
  }
</style>

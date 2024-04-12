<script lang="ts">
</script>

<div class="header">
  <h2><slot name="header" /></h2>
  <p><slot name="sub-header" /></p>
</div>
<div class="body">
  <slot name="body"/>
</div>

<style lang="scss">
  h2,
  p {
    margin: 0px;
  }

  div.header {
    margin: 8px 0px 8px 0px;
  }

  div.body {
    flex-grow: 1;

    display: flex;
    flex-direction: column;
    justify-content: center;

    margin: 8px 0px 8px 0px;
  }
</style>

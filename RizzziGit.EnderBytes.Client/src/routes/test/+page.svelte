<script lang="ts">
</script>

<div class="test">
  <div style="">Testas</div>
  <div style="">Test2</div>
</div>

<style lang="scss">
  div.test {
    padding: 16px;

    box-sizing: border-box;

    > div {
      width: 100%;
      height: 100%;

      box-sizing: border-box;

      background-color: black;
      color: white;

      float: left;
    }
  }
</style>

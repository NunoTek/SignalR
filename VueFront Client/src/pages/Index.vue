<template>
  <q-page class="flex flex-center">
    <!-- <img alt="Quasar logo" src="~assets/quasar-logo-full.svg"> -->
    <signalX></signalX>
  </q-page>
</template>

<script>
import SignalX from '../components/SignalX.vue'
export default {
  name: 'PageIndex',
  components: {
    SignalX
  }
}
</script>

<style lang="stylus" scoped>
</style>

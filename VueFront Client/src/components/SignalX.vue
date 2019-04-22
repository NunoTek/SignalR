<template>
  <div>
    <h1>Signal X</h1>

    <div class="publisher">
        <h2>Publisher</h2>
        <input type="text" id="reportName" placeholder="Enter report name" v-model="toReport"/>
        <input type="button" id="publishReport" value="Publish" @click="sendReport"/>
    </div>

    <div class="reports">
      <h2>Reports</h2>
      <ul id="reports">
        <li v-for="report in reports" :key="report">{{ report }}</li>
      </ul>
    </div>
  </div>
</template>

<script>
import urls from 'src/api-clients/baseUrls'
import * as signalR from '@aspnet/signalr'

export default {
  name: 'SignalX',
  data: () => {
    return {
      connection: null,
      toReport: '',
      reports: []
    }
  },
  methods: {
  connectSignalR () {
      let hubUrl = urls.signalR
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .configureLogging(signalR.LogLevel.Information)
        .build()

      this.connection.on('Close', this.onClosed)
      this.connection.on('Login', this.onLogin)
      this.connection.on('OnClientRequest', this.OnClientRequest)

      this.connection.start()

      console.log(this.connection)
    },
    onClosed (ex) {
      this.reports.push('Connection closed.')
    },
    onLogin () {
      let token = '' // TODO: ASK API FOR TOKEN
      this.connection.invoke('Authentificate', token)
    },
    sendReport () {
      let packet = {
          Header: {
              Item1: 0,
              Item2: 0,
              Item3: 0
          },
          Content: this.toReport,
          DateTime: new Date()
      }

      // Send to Hub
      this.connection.invoke('ClientToAll', packet);

      this.toReport = ''
    },
    OnClientRequest (packet) {
      console.log(packet)

      switch (packet.header.item1) {
        case 0: // State Change
          switch (packet.header.item2) {
            case 0: // Server
              this.reports.push(packet.content)
              break
            case 1: // Client
              this.reports.push(packet.content)
              break
          }
          break
      }
    }
  },
  mounted () {
    this.connectSignalR()
  }
}
</script>

<style lang="stylus" scoped>

</style>



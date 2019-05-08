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
import urls from '../configs/baseUrls.js'
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr'
// import * as signalR from '@aspnet/signalr'

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
    async connectSignalR () {
      let hubUrl = urls.signalR
      console.log('signalr server url:', hubUrl)
      this.connection = new HubConnectionBuilder()
        .withUrl(hubUrl)
        .configureLogging(LogLevel.Information)
        .build()

      console.log('connection:', this.connection)
      this.connection.on('Close', this.onClosed)
      this.connection.on('Receive', this.onReceive)
      this.connection.on('Login', this.onLogin)
      this.connection.on('OnClientRequest', this.onClientRequest)
      this.connection.on('OnServerRequest', this.onServerRequest)

      await this.connection.start().then(response => {
        console.log('connected:', response, this.connection)
      }).catch(ex => {
        console.error('error while connecting: ', ex)
      })
    },
    onClosed (ex) {
      console.log('closed', ex)
      this.reports.push('Connection closed.')

      this.connectSignalR()
    },
    onReceive (e, arg) {
      console.log('received', e, arg)
    },
    onLogin () {
      // TODO: ASK API FOR TOKEN
      let token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiIiwiZXhwIjoxNTU3MzE1MjY3LCJpc3MiOiJmcm9udC13ZWIiLCJhdWQiOiJiYWNrLWFwaSJ9.jjc3DOul_Ob-X8X7XUOuWhYz2qVuWWzlabjR1o6gSck'
      console.log('ask for login')
      this.connection.invoke('Authentificate', token)
    },
    sendReport () {
      let packet = {
        header: {
          Item1: 0,
          Item2: 0,
          Item3: 0
        },
        content: this.toReport,
        dateTime: new Date()
      }

      // Send to Hub
      this.connection.invoke('ClientToAll', packet)
      console.log('sent:', packet)
      this.reports.push(`ME : ${packet.content}`)

      this.toReport = ''
    },
    onServerRequest (packet) {
      console.log('server request: ', packet)
      this.reports.push(packet.content)
    },
    onClientRequest (sender, packet) {
      console.log('client request:', sender, packet)
      this.reports.push(`${sender} : ${packet.content}`)
    }
  },
  mounted () {
    this.connectSignalR()
  }
}
</script>

<style lang="stylus" scoped>
</style>

using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HubManager
{
    public class HubClient
    {
        #region " Properties "

        public string Token { get; set; }
        public bool PersistedConnection { get; set; } = true;
        public int RetryDelay { get; set; } = 10000;

        public HubConnectionState State { get { return hub.State; } }

        #endregion " Properties "

        private HubConnection hub;
        private string connectionUrl = "";

        public HubClient(string url)
        {
            connectionUrl = url;
        }

        public async Task ConnectAsync(string url = "")
        {
            if (!string.IsNullOrEmpty(url))
                connectionUrl = url;

            OnConnecting();

            try
            {
                hub = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .Build();

                Handlers();

                await hub.StartAsync();
            }
            catch (Exception exception)
            {
                OnError(exception);

                if (PersistedConnection)
                {
                    Thread.Sleep(RetryDelay);
                    await ConnectAsync();
                }
            }

            OnConnected();
        }

        public async Task DisconnectAsync(Exception exception)
        {
            OnError(exception);
            OnDisconnected();

            if (PersistedConnection)
            {
                Thread.Sleep(RetryDelay);
                await ConnectAsync();
            }
        }

        public void Handlers()
        {
            hub.Closed += (exception) => DisconnectAsync(exception);

            hub.On("Login", () =>
            {
                OnLog("Authentificate");
                hub.SendAsync("Authentificate", Token);
            });

            hub.On<Packet>("OnServerRequest", (request) =>
            {
                OnReceived("server", request);
            });

            hub.On<string, Packet>("OnClientRequest", (senderId, request) =>
            {
                OnReceived(senderId, request);
            });
        }

        public async Task SendToServerAsync(Packet request)
        {
            await hub.SendAsync("ClientToServer", request);
            OnSent("server", request);
        }

        public async Task SendToAllAsync(Packet request)
        {
            await hub.SendAsync("ClientToAll", request);
            OnSent("all", request);
        }

        public async Task SendToClientAsync(string clientId, Packet request)
        {
            await hub.SendAsync("ClientToClient", clientId, request);
            OnSent(clientId, request);
        }

        #region " Events "

        // Declare the delegate (if using non-generic pattern).
        public delegate void OnErrorHandler(object sender, Exception exception);

        // Declare the event.
        public event OnErrorHandler OnErrorRaised;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        protected virtual void OnError(Exception exception)
        {
            // Raise the event by using the () operator.
            if (OnErrorRaised != null)
            {
                OnLog($"Error occured: {exception.Message}");
                OnErrorRaised(this, exception);
            }
        }

        public delegate void OnLogHandler(object sender, string message);

        public event OnLogHandler OnLogRaised;

        protected virtual void OnLog(string message)
        {
            OnLogRaised?.Invoke(this, message);
        }

        public delegate void OnConnectingHandler(object sender, string url);

        public event OnConnectingHandler OnConnectingRaised;

        protected virtual void OnConnecting()
        {
            if (OnConnectingRaised != null)
            {
                OnConnectingRaised(this, connectionUrl);
                if (PersistedConnection)
                {
                    OnLog($"[{RetryDelay}ms] Reconnecting to: {connectionUrl}");
                }
                else
                {
                    OnLog($"[{RetryDelay}ms] Connecting to: {connectionUrl}");
                }
            }
        }

        public delegate void OnConnectedHandler(object sender);

        public event OnConnectedHandler OnConnectedRaised;

        protected virtual void OnConnected()
        {
            if (OnConnectedRaised != null)
            {
                OnConnectedRaised(this);
                OnLog($"Connected to: {connectionUrl}");
            }
        }

        public delegate void OnDisconnectedHandler(object sender);

        public event OnDisconnectedHandler OnDisconnectedRaised;

        protected virtual void OnDisconnected()
        {
            if (OnDisconnectedRaised != null)
            {
                OnDisconnectedRaised(this);
                OnLog($"Disconnected from: {connectionUrl}");
            }
        }

        public delegate void OnSentHandler(object sender, string clientId, Packet e);

        public event OnSentHandler OnSentRaised;

        protected virtual void OnSent(string clientId, Packet e)
        {
            if (OnSentRaised != null)
            {
                OnSentRaised(this, clientId, e);
                OnLog($"Packet sent to {clientId}: {e.DateTime}");
            }
        }

        public delegate void OnReceivedHandler(object sender, string clientId, Packet e);

        public event OnReceivedHandler OnReceivedRaised;

        protected virtual void OnReceived(string clientId, Packet e)
        {
            if (OnReceivedRaised != null)
            {
                OnLog($"Packet received from {clientId}: {e.DateTime}");
                OnReceivedRaised(this, clientId, e);
            }
        }

        #endregion " Events "
    }
}
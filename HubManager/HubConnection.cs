using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace HubManager
{
    public class HubConnection
    {
        #region " Properties "

        public string Token { get; set; }
        public bool PersistedConnection { get; set; } = true;
        public int RetryDelay { get; set; } = 5000;

        public HubConnectionState State { get { return hub.State; } }

        #endregion " Properties "

        public Microsoft.AspNetCore.SignalR.Client.HubConnection hub;
        private string connectionUrl = "";

        public HubConnection(string url)
        {
            connectionUrl = url;
        }

        #region " Helper "

        public class Packet
        {
            public Packet(Tuple<int, int, int> header = null, object content = null)
            {
                DateTime = DateTime.Now;
                Header = header;
                Content = content;
            }

            public DateTime DateTime { get; }
            public Tuple<int, int, int> Header { get; set; }
            public object Content { get; set; }
            //public int Length { get; set; }
        }

        #endregion " Helper "

        public async Task ConnectAsync(string url = "")
        {
            if (!string.IsNullOrEmpty(url))
                connectionUrl = url;

            try
            {
                hub = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .Build();

                Handlers();

                await hub.StartAsync();

                OnConnected();
            }
            catch (Exception exception)
            {
                OnError(exception);

                if (PersistedConnection)
                {
                    System.Threading.Thread.Sleep(RetryDelay);
                    await ConnectAsync();
                }
            }
        }

        public async Task DisconnectAsync(Exception exception)
        {
            OnError(exception);
            OnDisconnected();

            if (PersistedConnection)
            {
                System.Threading.Thread.Sleep(RetryDelay);
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
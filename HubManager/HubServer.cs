﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HubManager
{
    //[Authorize(Roles = "admin")]
    //[HasPermission("allrights")]
    public class HubServer : Hub
    {
        #region " WarningProperties "

        public bool WarningNewUserConnected { get; set; } = true;
        public bool WarningUsersConnected { get; set; } = true;
        public bool WarningUserDisconnected { get; set; } = true;

        #endregion " WarningProperties "

        public readonly ClientsManager clientsManager;

        public HubServer(IHttpContextAccessor accessor)
        {
            clientsManager = new ClientsManager(accessor);
        }

        #region " Clients Manager "

        public class ClientsManager
        {
            private volatile List<UserDto> _users;
            private IHttpContextAccessor _accessor;

            public ClientsManager(IHttpContextAccessor accessor)
            {
                _users = new List<UserDto>();
                _accessor = accessor;
            }

            public List<UserDto> Users()
            {
                return _users;
            }

            public async Task LoginAsync(UserDto user)
            {
                user.IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                _users.Add(user);
            }

            public async Task LogoutAsync(UserDto user)
            {
                _users.Remove(user);
                user.Dispose();
            }
        }

        public class UserDto : IDisposable
        {
            public UserDto(HubCallerContext context = null)
            {
                Context = context;
                Id = Guid.NewGuid().ToString();
                Language = "en-EN";
                ConnectionDate = DateTime.Now;
                Roles = new List<string>();
                Permissions = new List<string>();
            }

            [System.Runtime.Serialization.IgnoreDataMemberAttribute]
            public HubCallerContext Context { get; }

            public string Token { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string IpAddress { get; set; }
            public string Language { get; set; }
            public DateTime ConnectionDate { get; }

            public List<string> Roles { get; set; }
            public List<string> Permissions { get; set; }

            public void Dispose()
            {
                Roles.Clear();
                Permissions.Clear();
            }
        }

        #endregion " Clients Manager "

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

        #region " Exceptions "

        public class IntraConnectionExceptions
        {
            public const string UnauthorizedUser = "User unauthorized.";
            public const string VoidToken = "Token is void.";
            public const string ErrorEncountered = "An error occured, please contact an administrator";
        }

        #endregion " Exceptions "

        #region " Connection "

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var guestId = Context.ConnectionId;

            OnConnected(guestId);

            await Clients.Client(guestId).SendAsync("Login");
        }

        public async Task Authentificate(string token)
        {
            var guestId = Context.ConnectionId;

            if (string.IsNullOrEmpty(token))
                await DisconnectClient(guestId);

            var jwtSecurity = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var user = jwtSecurity?.Claims;
            if (jwtSecurity == null || user == null)
                await DisconnectClient(guestId);

            var roles = user.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value.Split(new string[] { "," }, StringSplitOptions.None);
            var userDto = new UserDto(Context)
            {
                Token = token,
                Id = guestId,
                Name = user.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Roles = roles.ToList(),
            };

            await clientsManager.LoginAsync(userDto);

            if (WarningUsersConnected)
            {
                var usersId = clientsManager.Users().Where(x => x.Id.Equals(guestId)).Select(x => x.Id);
                var request = new Packet(new Tuple<int, int, int>(0, 0, 0), $"Connected users {usersId.Count()} : {string.Join(", ", usersId)}");
                await ServerToClient(guestId, request);
            }

            if (WarningNewUserConnected)
            {
                var request = new Packet(new Tuple<int, int, int>(0, 0, 0), $"{DateTime.Now.ToString("D")} : New user connected : {guestId}");
                await ServerToAll(request);
            }
        }

        public async Task DisconnectClient(string clientId, string error = IntraConnectionExceptions.UnauthorizedUser)
        {
            if (string.IsNullOrEmpty(clientId))
                return;
            var client = clientsManager.Users().FirstOrDefault(x => x.Id == clientId);
            if (client == null)
                return;
            var ctx = client.Context;
            if (ctx == null)
                return;
            ctx.Abort();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var clientId = Context.ConnectionId;

            await base.OnDisconnectedAsync(ex);

            var userDto = clientsManager.Users().FirstOrDefault(x => x.Id == clientId);
            if (userDto != null)
            {
                await clientsManager.LogoutAsync(userDto);

                if (WarningNewUserConnected)
                {
                    var request = new Packet(new Tuple<int, int, int>(0, 0, 0), $"{DateTime.Now.ToString("D")} : User disconnected : {clientId}");
                    await ServerToAll(request);
                }
            }

            OnDisconnected(clientId);
        }

        #endregion " Connection "

        public bool AuthorizedUser(string clientId = "")
        {
            if (string.IsNullOrEmpty(clientId))
                clientId = Context.ConnectionId;
            return clientsManager.Users().Any(x => x.Id == clientId);
        }

        public async Task ClientToServer(Packet request)
        {
            //await Clients.Client(clientId).SendAsync("OnServerRequest", request);
            var senderId = Context.ConnectionId;
            OnReceived(senderId, request);
        }

        // Intranet

        public async Task ServerToAll(Packet request, bool includeGuests = false)
        {
            if (includeGuests)
            {
                await Clients.All.SendAsync("OnServerRequest", request);
                OnSent("server", "all+guests", request);
                return;
            }

            var usersIds = clientsManager.Users().Select(x => x.Id).ToList();
            await Clients.Clients(usersIds).SendAsync("OnServerRequest", request);
            OnSent("server", "all", request);
        }

        public async Task ServerToClient(string clientId, Packet request)
        {
            await Clients.Client(clientId).SendAsync("OnServerRequest", request);
            OnSent("server", clientId, request);
        }

        // Extranet

        public async Task ClientToAll(Packet request)
        {
            var senderId = Context.ConnectionId;
            if (!AuthorizedUser(senderId))
            {
                await DisconnectClient(senderId);
                return;
            }

            var usersIds = clientsManager.Users().Where(x => !x.Id.Equals(senderId)).Select(x => x.Id).ToList();
            await Clients.Clients(usersIds).SendAsync("OnClientRequest", senderId, request);
            OnSent(senderId, "all", request);
        }

        public async Task ClientToClient(string clientId, Packet request)
        {
            var senderId = Context.ConnectionId;
            if (!AuthorizedUser(senderId))
            {
                await DisconnectClient(senderId);
                return;
            }

            await Clients.Client(clientId).SendAsync("OnClientRequest", senderId, request);
            OnSent(senderId, clientId, request);
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

        protected virtual void OnConnected(string clientId)
        {
            if (OnConnectedRaised != null)
            {
                OnConnectedRaised(this);
                OnLog($"Connected to: {clientId}");
            }
        }

        public delegate void OnDisconnectedHandler(object sender);

        public event OnDisconnectedHandler OnDisconnectedRaised;

        protected virtual void OnDisconnected(string clientId)
        {
            if (OnDisconnectedRaised != null)
            {
                OnDisconnectedRaised(this);
                OnLog($"Disconnected from: {clientId}");
            }
        }

        public delegate void OnSentHandler(object sender, Packet e);

        public event OnSentHandler OnSentRaised;

        protected virtual void OnSent(string fromId, string toId, Packet e)
        {
            if (OnSentRaised != null)
            {
                OnSentRaised(this, e);
                OnLog($"Packet sent from {fromId} to {toId}: {e.DateTime}");
            }
        }

        public delegate void OnReceivedHandler(object sender, Packet e);

        public event OnReceivedHandler OnReceivedRaised;

        protected virtual void OnReceived(string clientId, Packet e)
        {
            if (OnReceivedRaised != null)
            {
                OnLog($"Packet received from {clientId}: {e.DateTime}");
                OnReceivedRaised(this, e);
            }
        }

        #endregion " Events "
    }
}
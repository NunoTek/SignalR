using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HubManager
{
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
}

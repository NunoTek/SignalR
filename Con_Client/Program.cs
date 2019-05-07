using HubManager;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace Con_Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var url = "http://localhost:5000/reportsPublisher";

            var token = GetToken(GetClaimsIdentity());
            var connection = new HubClient(url) { Token = token };

            HandleEvents(connection);

            connection.ConnectAsync().Wait();

            Thread.Sleep(500);

            if (connection.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
            {
                Console.WriteLine("[@ CMD] Awaiting your command.");

                var menu = Console.ReadLine();
                while (menu != "exit")
                {
                    var packet = new Packet(new Tuple<int, int, int>(0, 0, 0), menu);
                    connection.SendToAllAsync(packet).Wait();

                    menu = Console.ReadLine();
                }
            }
            else
            {

            }

            Console.WriteLine("Exiting");
            Thread.Sleep(500);
        }

        private static void HandleEvents(HubClient connection)
        {

            connection.OnLogRaised += (obj, log) =>
            {
                Console.WriteLine($"[i Log]: {log}");
            };

            connection.OnErrorRaised += (obj, ex) =>
            {
                Console.WriteLine($"[! Error] : {ex.Message}");
            };

            connection.OnConnectingRaised += (obj, url) =>
            {
                Console.WriteLine($"[* Connecting] to: {url}");
            };
            connection.OnConnectedRaised += (obj) =>
            {
                Console.WriteLine("[+ Connected]");
            };
            connection.OnDisconnectedRaised += (obj) =>
            {
                Console.WriteLine("[- Disconnected]");
            };

            connection.OnReceivedRaised += (obj, clientId, packet) =>
            {
                Console.WriteLine($"[-- Received]: [{packet.DateTime}][{clientId}]: {packet.Content.ToString()}");
            };

            connection.OnSentRaised += (obj, clientId, packet) =>
            {
                Console.WriteLine($"[++ Send]: [{packet.DateTime}][{clientId}]: {packet.Content.ToString()}");
            };
        }

        private static ClaimsIdentity GetClaimsIdentity()
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, ""),
                    new Claim(ClaimTypes.Role, ""),
                };

            return new ClaimsIdentity(claims);
        }

        private static string GetToken(ClaimsIdentity identity)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secretKey:HelloWorld"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = identity.Claims;
            var token = new JwtSecurityToken(
                issuer: "front-web",
                audience: "back-api",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

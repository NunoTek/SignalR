using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConCore_Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var url = "http://localhost:5000/reportsPublisher";

            Console.WriteLine("Connecting to {0}", url);

            var token = GetToken(GetClaimsIdentity());
            var connection = new HubManager.HubConnection(url) { Token = token };

            connection.OnLogRaised += (obj, log) =>
            {
                Console.WriteLine(log);
            };

            connection.OnReceivedRaised += (obj, clientId, packet) =>
            {
                Console.WriteLine($"[{packet.DateTime}][{clientId}]: {packet.Content.ToString()}");
            };

            connection.ConnectAsync().Wait();

            System.Threading.Thread.Sleep(500);

            if (connection.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
            {
                Console.WriteLine("Connected to {0}", url);
                Console.WriteLine("Awaiting your command.");

                var menu = Console.ReadLine();
                while (menu != "exit")
                {
                    var packet = new HubManager.HubConnection.Packet(new Tuple<int, int, int>(0, 0, 0), menu);
                    connection.SendToAllAsync(packet).Wait();

                    menu = Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Couldn't connect to {0}.", url);
            }

            Console.WriteLine("Exiting");
            System.Threading.Thread.Sleep(500);
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
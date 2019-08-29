using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Edays2019Chat.Services
{
    internal static class SignalRService
    {
        private static bool isInitialized;
        private static HubConnection connection;
        private static Subject<string> messageReceived = new Subject<string>();
        public static IObservable<string> MessageReceived = messageReceived.AsObservable();

        public static void Init()
        {
            if (isInitialized) return;

            connection = new HubConnectionBuilder()
               //.WithUrl("http://10.0.2.2:5000/chatHub")
               .WithUrl("https://apiservernoser.azurewebsites.net/chathub")
               .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
            isInitialized = true;
        }

        public static async Task Connect()
        {
            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                messageReceived.OnNext($"{user}: {message}");
            });

            try
            {
                await connection.StartAsync();
                messageReceived.OnNext("Connection started");
            }
            catch (Exception ex)
            {
                messageReceived.OnNext(ex.Message);
            }
        }

        public static async Task Send(string message)
        {
            try
            {
                await connection.SendAsync("SendMessage", "mobile-client", message);
            }
            catch (Exception ex)
            {
                messageReceived.OnNext(ex.Message);
            }
        }
    }

}

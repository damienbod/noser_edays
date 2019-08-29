using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;

namespace Wpf.Views
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public class MessageItem
        {
            public string Message { get; set; }
            public string Sender { get; set; }
            public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Black);
        }

        HubConnection connection;
        private string _connectionButtonText = "Connect";
        public string ConnectionButtonText
        {
            get => _connectionButtonText;
            set
            {
                if (_connectionButtonText == value) return;
                _connectionButtonText = value;
                RaisePropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            connection = new HubConnectionBuilder()
                .WithUrl("https://apiservernoser.azurewebsites.net/chathub")
                .WithAutomaticReconnect()
                .Build();

            connection.Reconnecting += error =>
            {
                Debug.Assert(connection.State == HubConnectionState.Reconnecting);

                // Notify users the connection was lost and the client is reconnecting.
                // Start queuing or dropping messages.

                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                Debug.Assert(connection.State == HubConnectionState.Connected);

                // Notify users the connection was reestablished.
                // Start dequeuing messages queued while reconnecting if any.

                return Task.CompletedTask;
            };

            #region snippet_ClosedRestart
            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
            #endregion
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionButtonText.Equals("Connect"))
            {
                #region snippet_ConnectionOn
                connection.On<string, string>("ReceiveMessage", (user, message) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        SolidColorBrush color = user.Equals(userTextBox.Text) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                        MessageItem mi = new MessageItem() { Sender = $"{user} wrote:", Message = message, Color = color };

                        messagesList.Items.Insert(0, mi);                      
                    });
                });
                #endregion

                try
                {
                    connectButton.IsEnabled = false;
                    await connection.StartAsync();
                    messagesList.Items.Add(new MessageItem() { Sender = "System", Message = "Connection started" });
                    sendButton.IsEnabled = true;
                    ConnectionButtonText = "Disconnect";
                    connectButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    messagesList.Items.Add(ex.Message);
                }
            }
            else
            {
                connectButton.IsEnabled = false;
                await connection.StopAsync();
                connectButton.IsEnabled = true;
                sendButton.IsEnabled = false;
                ConnectionButtonText = "Connect";
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            #region snippet_ErrorHandling
            try
            {
                #region snippet_InvokeAsync
                await connection.InvokeAsync("SendMessage",
                    userTextBox.Text, messageTextBox.Text);
                messageTextBox.Text = String.Empty;
                #endregion
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
            }
            #endregion
        }


        private void MessageBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                sendButton_Click(null, null);
        }

        private void RaisePropertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}

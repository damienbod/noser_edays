using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using Edays2019Chat.Models;
using Edays2019Chat.Views;
using Edays2019Chat.Services;

namespace Edays2019Chat.ViewModels
{
    public class ItemsViewModel : BaseViewModel, IDisposable
    {
        public ObservableCollection<Item> Items { get; set; }
        public Command LoadItemsCommand { get; set; }
        public Command ConnectCommand { get; set; }
        public Command SendMessageCommand { get; set; }

        private string _message;
        public string Message
        {
            get => _message;
            set { SetProperty(ref _message, value); }
        }

        private IDisposable subscription;

        public ItemsViewModel()
        {
            Title = "Chat";
            Items = new ObservableCollection<Item>();

            SignalRService.Init();
            subscription = SignalRService.MessageReceived.Subscribe(s => Items.Add(new Item { Text = s, Description = "SignalR message" }));

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            ConnectCommand = new Command(async () => await ExecuteConnectCommand());
            SendMessageCommand = new Command(async () => await ExecuteSendMessageCommand());

            MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
            {
                var newItem = item as Item;
                Items.Add(newItem);
                await DataStore.AddItemAsync(newItem);
            });
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await DataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task ExecuteConnectCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await SignalRService.Connect();
                Items.Clear();

                var items = await DataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task ExecuteSendMessageCommand()
        {
            await SignalRService.Send(Message);
            Message = "";
        }

        public void Dispose()
        {
            subscription?.Dispose();
        }
    }
}
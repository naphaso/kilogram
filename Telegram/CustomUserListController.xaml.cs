using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram
{
    public partial class CustomUserListController : UserControl
    {
        private ObservableCollection<UserModel> userCollection = new ObservableCollection<UserModel>();
        private int _chatId = 0;
        public event OnUserSelected UserSelected;
        public CustomUserListController()
        {
            InitializeComponent();
            UserListSelector.Visibility = Visibility.Collapsed;
            UserListSelector.ItemsSource = userCollection;
        }

        public void UpdateChatWithId(int chatId) {
            GetChatParticipants(chatId);
        }

        private async Task GetChatParticipants(int chatId) {
            await TelegramSession.Instance.Established;

            messages_ChatFull chatFull = await TelegramSession.Instance.Api.messages_getFullChat(chatId);
            Messages_chatFullConstructor chatFullCons = (Messages_chatFullConstructor) chatFull;
            
            List<User> chatUsers = chatFullCons.users;
            TelegramSession.Instance.Updates.ProcessUsers(chatUsers);

            foreach (var chatUser in chatUsers) {
                int id = 0;

                switch (chatUser.Constructor) {
                    case Constructor.userDeleted:
                        id = ((UserDeletedConstructor) chatUser).id;
                        break;

                        case Constructor.userEmpty:
                        id = ((UserEmptyConstructor) chatUser).id;
                        break;

                        case Constructor.userContact:
                        id = ((UserContactConstructor) chatUser).id;
                        break;

                        case Constructor.userRequest:
                        id = ((UserRequestConstructor) chatUser).id;
                        break;

                        case Constructor.userForeign:
                        id = ((UserForeignConstructor) chatUser).id;
                        break;

                        case Constructor.userSelf:
                        id = ((UserSelfConstructor) chatUser).id;
                        break;
                }

                userCollection.Add(TelegramSession.Instance.GetUser(id));
            }

            if (userCollection.Count > 0) {
                UserListSelector.Visibility = Visibility.Visible;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ((sender as LongListSelector).SelectedItem == null)
                return;

            UserModel selectedUser = (sender as LongListSelector).SelectedItem as UserModel;
            if (UserSelected != null)
                UserSelected(this, selectedUser);

            (sender as LongListSelector).SelectedItem = null;
        }

    }

    public delegate void OnUserSelected(object sender, UserModel userModel);
}

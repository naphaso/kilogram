using System;
using System.Collections.Generic;
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

namespace Telegram.UI.Pages {
    public partial class NewSecretChat : PhoneApplicationPage {
        public NewSecretChat() {
            InitializeComponent();
            UserList.ContactsList.Visibility = Visibility.Collapsed;
        }

        private void OnUserSelected(object sender, UserModel user) {
            Task.Run(() => TelegramSession.Instance.EncryptedChats.CreateChatRequest(user.InputUser));
            NavigationService.Navigate(new Uri("/UI/Pages/StartPage.xaml", UriKind.Relative));
        }
    }
}
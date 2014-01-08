using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model.Wrappers;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI.Pages {
    public partial class ChatCreate : PhoneApplicationPage {
        public ChatCreate() {
            InitializeComponent();

            UserControl.ContactsList.Visibility = Visibility.Collapsed;
            UserControl.TelegramUserSelected += UserControlOnTelegramUserSelected;
        }

        private void UserControlOnTelegramUserSelected(object sender, UserModel user) {
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?userId=" + user.Id, UriKind.Relative));
        }

        private void NewGroupTap(object sender, GestureEventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/NewGroup.xaml", UriKind.Relative));
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e) {
            string searchQuery = SearchBox.Text.ToLower();

            UserControl.FilterTelegramUsersByName(searchQuery);
        }
    }
}
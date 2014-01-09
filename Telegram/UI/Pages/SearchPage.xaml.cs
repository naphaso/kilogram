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
using Telegram.MTProto;

namespace Telegram.UI.Pages {
    public partial class SearchPage : PhoneApplicationPage {
        public SearchPage() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            SearchTextBox.Focus();
        }

        private void SeachTextChanged(object sender, TextChangedEventArgs e) {
            DialogsList.Filter(SearchTextBox.Text);
        }

        private void OnDialogSelected(object sender, DialogModel model) {
            int modelId = TelegramSession.Instance.Dialogs.Model.Dialogs.IndexOf(model);
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?modelId=" + modelId, UriKind.Relative));
        }
    }
}
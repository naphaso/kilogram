using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.UI.Controls;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;

namespace Telegram.UI
{
    public partial class StartPage : PhoneApplicationPage
    {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(StartPage));

        private static DialogsModel _dialogs = null;

        public static DialogsModel Dialogs {
            get { return _dialogs ?? (_dialogs = new DialogsModel()); }
        }

        public StartPage()
        {
            InitializeComponent();

            this.BackKeyPress += delegate {
                Application.Current.Terminate();
            };

            DialogList.DialogSelected += delegate(object sender, DialogModel model) {
                int modelId = TelegramSession.Instance.Dialogs.Model.Dialogs.IndexOf(model);
                
//                logger.debug("Selected dialog with user/chat ID=" + userId);
                NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?modelId=" + modelId, UriKind.Relative));
            };
        }

        private void New_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml", UriKind.Relative));
        }

        private void Search_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/SearchPage.xaml", UriKind.Relative));
        }

        private void Settings_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/Settings.xaml", UriKind.Relative));
        }
    }
}
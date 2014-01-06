using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model;

namespace Telegram.UI
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            // FIXME: hardcoded model
            if (App.SettingsModel == null) {
                App.SettingsModel = new MainSettingsModel();
                App.SettingsModel.Init();
            }

            // get data model from Telegram API settings
            this.DataContext = App.SettingsModel;
        }

        private void Edit_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Dummy_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void SettingsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender.GetType() != typeof (LongListSelector))
                return;

            var selector = (LongListSelector) sender;

            if (selector.SelectedItem.GetType() != typeof (MainSettingsItem))
                return;

            var item = (MainSettingsItem) selector.SelectedItem;

            if (item.Name == "notifications") {
                Debug.WriteLine("Selected notifications");
                NavigationService.Navigate(new Uri("/UI/Pages/SettingsNotification.xaml", UriKind.Relative));
            }
            else {
                Debug.WriteLine("Uknown selection");
            }
            
            
        }
    }
}
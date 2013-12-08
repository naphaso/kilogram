using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private void SavePhotos_Checked(object sender, RoutedEventArgs e) {
            Debug.WriteLine("SavePhotos_Checked");
        }

        private void SavePhotos_Unchecked(object sender, RoutedEventArgs e) {
            Debug.WriteLine("SavePhotos_Unchecked");
        }
    }
}
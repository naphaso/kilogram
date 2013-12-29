using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;

namespace Telegram.UI
{
    public partial class StartPage : PhoneApplicationPage
    {
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
        }

        public void EnableSearchMode() {
            
        }

        public void DisableSearchMode() {
            
        }

        private void BuildAppBar() {
            ApplicationBar = new ApplicationBar();
            
            var newButton = new ApplicationBarIconButton(new Uri("/Assets/UI/appbar.new.png", UriKind.Relative));
            newButton.Text = "Create";

            var searchButton = new ApplicationBarIconButton(new Uri("/Assets/UI/appbar.feature.search.png", UriKind.Relative));
            searchButton.Text = "Search";

            ApplicationBar.Buttons.Add(newButton);
            ApplicationBar.Buttons.Add(searchButton);

            ApplicationBar.MenuItems.Add(new ApplicationBarMenuItem("Settings"));
        }

        private void New_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/ChatCreate.xaml", UriKind.Relative));
        }

        private void Search_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/SearchPage.xaml", UriKind.Relative));
        }

        private void Settings_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/Settings.xaml", UriKind.Relative));
        }
    }
}
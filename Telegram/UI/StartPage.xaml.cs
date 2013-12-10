using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            RemoveBackStack();
            var items = new ObservableCollection<DialogItem> {
                new DialogItem() {Avatar = "1", Preview = "Hello.", Timestamp = "11:21a", Title = "John Doe"},
                new DialogItem() {Avatar = "2", Preview = "Hi there!", Timestamp = "9:56a", Title = "Jane Doe"},
                new DialogItem() {Avatar = "3", Preview = "Stay awhile and listen.", Timestamp = "1:21a", Title = "Decard Kain"}
            };

            dialogList.ItemsSource = items;

            items.Add(new DialogItem() { Avatar = "4", Preview = "Stay awhile and listen.", Timestamp = "1:21a", Title = "Decard Kain" });
            dialogList.ItemsSource = new ObservableCollection<DialogItem>(items.OrderBy(i => i.Timestamp));

            var users = new List<UserItem> {
                new UserItem() {Name = "John Doe", Online = true},
                new UserItem() {Name = "Jane Doe", Online = true},
                new UserItem() {Name = "Decard Kain", Online = false, LastSeen = "19:33p"},
                new UserItem() {Name = "Stanislav Ovsyannikov", AddressBookContact = true}
            };

            List<AlphaKeyGroup<UserItem>> userDataSource = AlphaKeyGroup<UserItem>.CreateGroups(users,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                (UserItem s) => s.Name, true);

            var observableUsersSource = new ObservableCollection<AlphaKeyGroup<UserItem>>(userDataSource);
            
            contactsList.ItemsSource = observableUsersSource;

//            BuildAppBar();
        }

        private void RemoveBackStack() {
            while (NavigationService.CanGoBack) 
                NavigationService.RemoveBackEntry();
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
            throw new NotImplementedException();
        }

        private void Search_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Settings_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Settings.xaml", UriKind.Relative));
        }
    }
}
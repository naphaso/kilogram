using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;

namespace Telegram.UI.Controls {
    public partial class UserListControl : UserControl {
        public UserListControl() {
            InitializeComponent();

            initDemo();
        }

        private void initDemo() {
            var users = new List<UserItem> {
                new UserItem() {Name = "John Doe", Online = true},
                new UserItem() {Name = "Jane Doe", Online = true},
                new UserItem() {Name = "Decard Kain", Online = false, LastSeen = "19:33p"},
                new UserItem() {Name = "Igor Glotov", Online = true, LastSeen = "19:33p"},
                new UserItem() {Name = "Mila Kunis", Online = false, LastSeen = "19:33p"},
                new UserItem() {Name = "Jack Daniel", Online = false, LastSeen = "19:33p"},
                new UserItem() {Name = "Stanislav Ovsyannikov", AddressBookContact = true}
            };

            List<AlphaKeyGroup<UserItem>> userDataSource = AlphaKeyGroup<UserItem>.CreateGroups(users,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                (UserItem s) => s.Name, true);

            var observableUsersSource = new ObservableCollection<AlphaKeyGroup<UserItem>>(userDataSource);

            ContactsList.ItemsSource = observableUsersSource;
        }


    }
}

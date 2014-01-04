using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;

namespace Telegram.UI.Controls {
    public partial class UserListControl : UserControl {
        public class UserUiModel {
            public string Name { get; set; }
        }

        public event OnTelegramUserSelected TelegramUserSelected;
        public event OnAddressbookUserSelected AddressbookUserSelected;

        private ObservableCollection<UserUiModel> userList = new ObservableCollection<UserUiModel>();
        public UserListControl() {
            InitializeComponent();

            Contacts contacts = new Contacts();

            contacts.SearchCompleted += ContactsOnSearchCompleted;
            contacts.SearchAsync(String.Empty, FilterKind.None, "Addressbook Contacts");
            initDemo();
        }

        private void ContactsOnSearchCompleted(object sender, ContactsSearchEventArgs e) {
            var items = e.Results;
//                from Contact con in e.Results
//                from ContactPhoneNumber phone in con.PhoneNumbers
//                where phone != null
//                select con;
            List<Contact> contactsWithPhones = new List<Contact>();
            foreach (var contact in items) {
                if (contact.PhoneNumbers.Count() != 0) {
                    contactsWithPhones.Add(contact);
                }
            }

            List<AlphaKeyGroup<Contact>> userDataSource = AlphaKeyGroup<Contact>.CreateGroups(contactsWithPhones,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                (Contact s) => s.DisplayName, true);
            var observableUsersSource = new ObservableCollection<AlphaKeyGroup<Contact>>(userDataSource);
            ContactsList.ItemsSource = observableUsersSource;
//            ContactsList.DataContext = items;
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

//            List<AlphaKeyGroup<UserItem>> userDataSource = AlphaKeyGroup<UserItem>.CreateGroups(users,
//                System.Threading.Thread.CurrentThread.CurrentUICulture,
//                (UserItem s) => s.Name, true);

//            var observableUsersSource = new ObservableCollection<AlphaKeyGroup<UserItem>>(userDataSource);

            FriendsList.ItemsSource = users;
        }


        private void AddressbookContactSelected(object sender, SelectionChangedEventArgs e) {
            var contactsList = sender as LongListSelector;
            Debug.Assert(contactsList != null, "contactsList != null");
            Contact selectedContact = contactsList.SelectedItem as Contact;

            Debug.Assert(selectedContact != null, "selectedContact != null");
            AddressbookUserSelected(sender, selectedContact);
        }
    }

    public delegate void OnAddressbookUserSelected(object sender, Contact contact);

    public delegate void OnTelegramUserSelected(object sender, int userId);
}

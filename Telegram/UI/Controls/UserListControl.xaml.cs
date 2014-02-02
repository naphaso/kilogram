using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Windows.Foundation.Metadata;
using Windows.Phone.PersonalInformation;
using Windows.Storage.Streams;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;
using Contact = Microsoft.Phone.UserData.Contact;

namespace Telegram.UI.Controls {
    public partial class UserListControl : UserControl {
        public class UserUiModel {
            public string Name { get; set; }
        }

        public event OnTelegramUserSelected TelegramUserSelected;
        public event OnAddressbookUserSelected AddressbookUserSelected;

        private ObservableCollection<UserModel> friendList = new ObservableCollection<UserModel>(); 

        private ObservableCollection<UserUiModel> userList = new ObservableCollection<UserUiModel>();
        public UserListControl() {
            InitializeComponent();
            Contacts contacts = new Contacts();

            contacts.SearchCompleted += ContactsOnSearchCompleted;
            contacts.SearchAsync(String.Empty, FilterKind.None, "Addressbook Contacts");

            FriendsList.ItemsSource = friendList;

            GetFriends();
//            initDemo();
        }

        public void FilterTelegramUsersByName(string filter) {
            if (filter == "") {
                FriendsList.ItemsSource = friendList;
                return;
            }

//            foreach (var userModel in friendList) {
//                if (userModel.FullName.ToLower().Contains(filter)) {
//                    filteredCollection.Add(userModel);
//                }
//            }
//            
            var result = friendList.Where(c => c.FullName.ToLower().Contains(filter));
            FriendsList.ItemsSource = result.ToList();
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

        private async void GetFriends() {
            ContactStore store = await ContactStore.CreateOrOpenAsync();
            ContactQueryResult result = store.CreateContactQuery();



            IReadOnlyList<StoredContact> contacts = await result.GetContactsAsync();

            friendList.Clear();
            
            foreach (var storedContact in contacts) {
                UserModel user = TelegramSession.Instance.GetUser(int.Parse(storedContact.RemoteId));
                friendList.Add(user);                
            }

            if (contacts.Count > 0)
                FriendsList.Visibility = Visibility.Visible;
        }

        private void AddressbookContactSelected(object sender, SelectionChangedEventArgs e) {
            if ((sender as LongListSelector).SelectedItem == null)
                return;

            var contactsList = sender as LongListSelector;
            Contact selectedContact = contactsList.SelectedItem as Contact;

            if (AddressbookUserSelected != null)
                AddressbookUserSelected(sender, selectedContact);

            (sender as LongListSelector).SelectedItem = null;
        }

        private void ContactSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ((sender as LongListSelector).SelectedItem == null)
                return;

            var userModel = (sender as LongListSelector).SelectedItem as UserModel;

            if (TelegramUserSelected != null)
                TelegramUserSelected(sender, userModel);

            (sender as LongListSelector).SelectedItem = null;
        }
    }

    public delegate void OnAddressbookUserSelected(object sender, Contact contact);

    public delegate void OnTelegramUserSelected(object sender, UserModel user);
}

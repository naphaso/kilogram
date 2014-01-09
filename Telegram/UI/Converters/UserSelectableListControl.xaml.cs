using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
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
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI.Controls {
    public partial class UserSelectableListControl : UserControl {

        private ObservableCollection<UserModel> friendList = new ObservableCollection<UserModel>();
        public event OnUserSelectedFromList UserSelectedFromList;
        public UserSelectableListControl() {
            InitializeComponent();
            FriendsList.ItemsSource = friendList;

            GetFriends();
        }

        public void FilterUsersByName(string filter) {
            if (filter == "")
                FriendsList.ItemsSource = friendList;

            filter = filter.Trim().ToLower();
            ObservableCollection<UserModel> filteredCollection = new ObservableCollection<UserModel>();

            foreach (var userModel in friendList) {
                if (userModel.FullName.ToLower().Contains(filter)) {
                    filteredCollection.Add(userModel);
                }
            }

            FriendsList.ItemsSource = filteredCollection;
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

           
        }

        public ObservableCollection<UserModel> GetUsers() {
            return friendList;
        }

        private void SelectionBox_OnTap(object sender, GestureEventArgs e) {
            if (UserSelectedFromList != null) {
                UserSelectedFromList(this, sender);
            }
        }
    }

    public delegate void OnUserSelectedFromList(object sender, object args);
}

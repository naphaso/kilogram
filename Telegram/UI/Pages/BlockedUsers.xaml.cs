using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.UI.Pages
{
    public partial class BlockedUsers : PhoneApplicationPage
    {
        private ProgressIndicator progressIndicator;

        private ObservableCollection<UserModel> blockedUsers = new ObservableCollection<UserModel>(); 
        public BlockedUsers()
        {
            InitializeComponent();
            LoadBlockedUsers();

            BlockedUserListSelector.ItemsSource = blockedUsers;
        }

        // FIXME: update maximum 10 blocked users to unlimited
        private async Task LoadBlockedUsers() {
            ShowProgress();
            blockedUsers.Clear();
            contacts_Blocked contactsBlocked = await TelegramSession.Instance.Api.contacts_getBlocked(0, 100);

            if (contactsBlocked.Constructor == Constructor.contacts_blocked) {
                Contacts_blockedConstructor cbc = (Contacts_blockedConstructor) contactsBlocked;
                
                TelegramSession.Instance.Updates.ProcessUsers(cbc.users);
                ProcessBlockedUsersList(cbc.blocked);
            }
            else if (contactsBlocked.Constructor == Constructor.contacts_blockedSlice) {
                Contacts_blockedSliceConstructor cbsc = (Contacts_blockedSliceConstructor) contactsBlocked;

                TelegramSession.Instance.Updates.ProcessUsers(cbsc.users);
                ProcessBlockedUsersList(cbsc.blocked);
            }

            HideProgress();
        }

        private void ProcessBlockedUsersList(List<ContactBlocked> contacts) {
            foreach (var contactBlocked in contacts)
            {
                ContactBlockedConstructor contactBlockedConstructor = (ContactBlockedConstructor)contactBlocked;

                UserModel blockedUser = TelegramSession.Instance.GetUser(contactBlockedConstructor.user_id);
                blockedUsers.Add(blockedUser);
            }
        }

        private void HideProgress() {
            progressIndicator.IsVisible = false;
        }

        private void ShowProgress() {
            this.IsEnabled = false;

            if (progressIndicator == null) {
                progressIndicator = new ProgressIndicator();
                SystemTray.SetProgressIndicator(this, progressIndicator);
            }

            progressIndicator.IsIndeterminate = true;
            progressIndicator.IsVisible = true;
        }

        private void BlockedUserSelected(object sender, SelectionChangedEventArgs e) {
            
        }

        private void OnUnblockClick(object sender, RoutedEventArgs e) {
            var model = ((sender as MenuItem).DataContext as UserModel);

            DoUnblock(model);

        }

        private async void DoUnblock(UserModel model) {
            ShowProgress();
            bool result = await TelegramSession.Instance.Api.contacts_unblock(model.InputUser);
            
            if (result) {
                blockedUsers.Remove(model);
            } else {
                Toaster.ShowNetworkError();
            }

            HideProgress();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.Platform;
using Telegram.UI.Controls;
using Telegram.UI.Models;
using Telegram.UI.Models.Users;
using Contact = Microsoft.Phone.UserData.Contact;

namespace Telegram.UI
{
    public partial class StartPage : PhoneApplicationPage
    {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(StartPage));

        private static DialogsModel _dialogs = null;

        public static DialogsModel Dialogs {
            get { return _dialogs ?? (_dialogs = new DialogsModel()); }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            if (TelegramSession.Instance.AuthorizationExists()) {
                Task.Run(() => TelegramSession.Instance.ConnectAsync());
            } else {
                NavigationService.Navigate(new Uri("/UI/Pages/Signup.xaml", UriKind.Relative));
            }
        }

        public StartPage()
        {
            this.Loaded += OnLoaded;
            if (!TelegramSession.Instance.AuthorizationExists()) {
                return;
            }

            TelegramSession.Instance.Dialogs.OpenedDialog = null;

            InitializeComponent();
            this.BackKeyPress += delegate {
                TelegramSession.Instance.save();
                TelegramSession.Instance.GoToOffline().ContinueWith((res) => Application.Current.Terminate());
                Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith((res) => Application.Current.Terminate());
            };

            

            DialogList.DialogSelected += delegate(object sender, DialogModel model) {
                int modelId = TelegramSession.Instance.Dialogs.Model.Dialogs.IndexOf(model);
                NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?modelId=" + modelId, UriKind.Relative));
            };

            ContactList.AddressbookUserSelected += ContactListOnAddressbookUserSelected;
            ContactList.TelegramUserSelected += ContactListOnTelegramUserSelected;


        }

        private void ContactListOnTelegramUserSelected(object sender, UserModel user) {
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?userId=" + user.Id, UriKind.Relative));
        }

        private void ContactListOnAddressbookUserSelected(object sender, Contact contact) {
            InviteContactAsync(contact);
        }

        private async Task InviteContactAsync(Contact contact) {
            try {
                ContactsProgressBar.Visibility = Visibility.Visible;
                help_InviteText text = await TelegramSession.Instance.Api.help_getInviteText("en");
                Help_inviteTextConstructor textCtor = (Help_inviteTextConstructor) text;

                SmsComposeTask smsComposeTask = new SmsComposeTask();

                smsComposeTask.To = contact.PhoneNumbers.ToList()[0].PhoneNumber;
                smsComposeTask.Body = textCtor.message;
                smsComposeTask.Show();

                ContactsProgressBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        private void New_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/ChatCreate.xaml", UriKind.Relative));
        }

        private void Search_Click(object sender, EventArgs e) {
            //NavigationService.Navigate(new Uri("/UI/Pages/SearchPage.xaml", UriKind.Relative));
            Task.Run(() => TelegramSession.Instance.EncryptedChats.CreateChatRequest(TL.inputUserContact(117870))); // 117870, 246813
        }

        private void Settings_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/Settings.xaml", UriKind.Relative));
        }
    }
}
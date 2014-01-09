using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.UI.Pages {
    public partial class NewGroup : PhoneApplicationPage {
        public NewGroup() {
            InitializeComponent();

            ChatTitleEdit.TitleText.Text = "Subject:";
            ChatTitleEdit.ContentText.Text = "New chat";
            ChatMembersEdit.TitleText.Text = "Members:";
            ChatMembersEdit.ContentText.Text = "";

            ChatMembersEdit.ContentText.TextChanged += ContentTextOnTextChanged;
            ChatMembersEdit.ContentText.GotFocus += ContentTextOnGotFocus;
            SelectableUsers.UserSelectedFromList += SelectableUsersOnUserSelectedFromList;
           
        }

        private void ContentTextOnGotFocus(object sender, RoutedEventArgs routedEventArgs) {
            ChatMembersEdit.ContentText.Select(ChatMembersEdit.ContentText.Text.Length, 0);
        }

        private void SelectableUsersOnUserSelectedFromList(object sender, object args) {
            UpdateParticipantsText();
        }

        private void ContentTextOnTextChanged(object sender, TextChangedEventArgs textChangedEventArgs) {
            string fullText = ChatMembersEdit.ContentText.Text;

            if (fullText.LastIndexOf(";") == -1) {
                SelectableUsers.FilterUsersByName(fullText);
                return;
            }

            if (fullText.LastIndexOf(";") == fullText.Length - 1) {
                SelectableUsers.FilterUsersByName("");
                return;
            }

            string currentMember = fullText.Substring(fullText.LastIndexOf(";")+1);
            SelectableUsers.FilterUsersByName(currentMember);
        }

        private void UpdateParticipantsText() {
            string fullText = ChatMembersEdit.ContentText.Text;
            if (fullText.LastIndexOf(";") == -1) {
                ChatMembersEdit.ContentText.Text = GetSelectedUserList();
                return;
            }

            string currentMember = fullText.Substring(fullText.LastIndexOf(";")+1);
            ChatMembersEdit.ContentText.Text = GetSelectedUserList() + currentMember;
        }

        public string GetSelectedUserList() {
            ObservableCollection<UserModel> users = SelectableUsers.GetUsers();
            string text = "";

            foreach (var userModel in users) {
                if (userModel.IsCheckedInternal)
                    text += " " + userModel.FullName + ";";
            }

            return text;
        }

        private void FinishClick(object sender, EventArgs e) {
            if (ChatTitleEdit.ContentText.Text == "") {
                ChatTitleEdit.ContentText.Focus();
                return;
            }

            ObservableCollection<UserModel> users = SelectableUsers.GetUsers();

            bool haveUsers = false;
            foreach (var userModel in users) {
                if (userModel.IsCheckedInternal) { 
                    haveUsers = true;
                    break;
                }
            }

            if (!haveUsers) {
                ChatMembersEdit.ContentText.Focus();
                return;
            }

            DoCreateChat();
            NavigationService.Navigate(new Uri("/UI/Pages/StartPage.xaml", UriKind.Relative));
        }

        private async Task DoCreateChat() {
            List<InputUser> inputUsers = new List<InputUser>();
            ObservableCollection<UserModel> users = SelectableUsers.GetUsers();

            foreach (var userModel in users) {
                if (userModel.IsCheckedInternal)
                    inputUsers.Add(TL.inputUserContact(userModel.Id));
            }

            messages_StatedMessage msg = await TelegramSession.Instance.Api.messages_createChat(inputUsers, ChatTitleEdit.ContentText.Text);
            TelegramSession.Instance.Updates.Process(msg);
        }
    }
}
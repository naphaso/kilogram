using System;
using System.Collections.Generic;
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
    public partial class SendContact : PhoneApplicationPage {
        private DialogModel dialogToReturn;

        public SendContact() {
            InitializeComponent();
            UserList.ContactsList.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("modelId", out uriParam)) {
                dialogToReturn = TelegramSession.Instance.Dialogs.Model.Dialogs[(int.Parse(uriParam))];
            } 
        }

        private void OnUserSelected(object sender, UserModel user) {
            DoSendContact(user);
            NavigationService.GoBack();
        }

        private async Task DoSendContact(UserModel user) {
            if (!(dialogToReturn is DialogModelPlain))
                return;

            InputPeer ip = dialogToReturn.InputPeer;
            InputMedia media = TL.inputMediaContact(user.PhoneNumber, user.FirstName, user.LastName);

            DialogModelPlain dialog = (DialogModelPlain) dialogToReturn;
            await dialog.SendMedia(media);
        }
    }
}
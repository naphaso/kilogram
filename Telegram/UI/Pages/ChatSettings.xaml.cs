using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.UI.Pages {
    public partial class ChatSettings : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(UserProfile));
        private ChatModel model = null;
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";
            
            if (NavigationContext.QueryString.TryGetValue("chatId", out uriParam)) {
                model = TelegramSession.Instance.GetChat(int.Parse(uriParam));
            } else {
                logger.error("Unable to get model id from navigation");
            }

            UpdateDataContext();
        }
        private void UpdateDataContext() {
            this.DataContext = model;
            UserListController.UpdateChatWithId(model.Id);
        }

        public ChatSettings() {
            InitializeComponent();
        }

        private void Add_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Edit_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void OnUserSelected(object sender, UserModel usermodel) {
            NavigationService.Navigate(new Uri("/UI/Pages/UserProfile.xaml?userId=" + usermodel.Id, UriKind.Relative));
        }
    }
}
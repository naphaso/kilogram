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

namespace Telegram.UI {
    public partial class EditNamePage : PhoneApplicationPage {
        public EditNamePage() {
            InitializeComponent();

            UserModel model = TelegramSession.Instance.GetUser(TelegramSession.Instance.SelfId);
            FirstNameControl.TitleText.Text = "First Name:";
            FirstNameControl.ContentText.Text = model.FirstName;

            LastNameControl.TitleText.Text = "Last Name:";
            LastNameControl.ContentText.Text = model.LastName;

        }

        private async void OnDoneClick(object sender, EventArgs e) {
            FinishJob();
            NavigationService.Navigate(new Uri("/UI/Pages/Settings.xaml", UriKind.Relative));
        }

        private async Task FinishJob() {
            User user = await TelegramSession.Instance.Api.account_updateProfile(FirstNameControl.ContentText.Text,
    LastNameControl.ContentText.Text);

            TelegramSession.Instance.Updates.ProcessUsers(new List<User>() { user });
        }
    }
}
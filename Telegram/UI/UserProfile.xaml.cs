using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram.UI {
    public partial class UserProfile : PhoneApplicationPage {
        public UserProfile() {
            InitializeComponent();
        }

        private void Share_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Edit_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/EditUserProfile.xaml", UriKind.Relative));
        }

        private void Block_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }
    }
}
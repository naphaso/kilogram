using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram.UI {
    public partial class EditUserProfile : PhoneApplicationPage {
        public EditUserProfile() {
            InitializeComponent();
            NameButtonUserControl.Header.Text = "name";
            NameButtonUserControl.Content.Text = "John Doe";

            PhoneButtonUserControl.Header.Text = "mobile phone";
            PhoneButtonUserControl.Content.Text = "+7 923 412 49 40";
        }

        private void AddPhoneRecord(object sender, MouseButtonEventArgs e) {
            throw new NotImplementedException();
        }

        private void EditName(object sender, MouseButtonEventArgs e) {
            NavigationService.Navigate(new Uri("/UI/EditNamePage.xaml", UriKind.Relative));
        }
    }
}
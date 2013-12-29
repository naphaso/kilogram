using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI.Pages {
    public partial class ChatCreate : PhoneApplicationPage {
        public ChatCreate() {
            InitializeComponent();
        }

        private void NewGroupTap(object sender, GestureEventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/NewGroup.xaml", UriKind.Relative));
        }
    }
}
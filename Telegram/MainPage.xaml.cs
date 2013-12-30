using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.mtproto;
using Telegram.MTProto;
using Telegram.Resources;

namespace Telegram
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;

        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            if (TelegramSession.Instance.AuthorizationExists()) {
                TelegramSession.Instance.ConnectAsync();
                NavigationService.Navigate(new Uri("/UI/Pages/StartPage.xaml", UriKind.Relative));
            } else {
                NavigationService.Navigate(new Uri("/UI/Pages/Signup.xaml", UriKind.Relative));
            }
        }
    }
}
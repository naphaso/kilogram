using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram.UI.Pages {
    public partial class SearchPage : PhoneApplicationPage {
        public SearchPage() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            SearchTextBox.Focus();
        }

        private void SeachTextChanged(object sender, TextChangedEventArgs e) {
            
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram {
    public partial class SignupPhone : PhoneApplicationPage {
        int screenState = 0;
        public SignupPhone() {
            InitializeComponent();
            showPhoneScene();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e) {
            switch (screenState) {
                case 0:
                    showCodeScene();
                    break;
                case 1:
                    showNameScene();
                    break;
                case 2:
                    NavigationService.Navigate(new Uri("/UI/DialogList.xaml", UriKind.Relative));
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown screen state");
                    break;
            }

            screenState++;
        }

        private void showPhoneScene() {
            phoneControl.Visibility = System.Windows.Visibility.Visible;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void showCodeScene() {
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Visible;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void showNameScene() {
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
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
        int _screenState = 0;
        public SignupPhone() {
            InitializeComponent();
            ShowPhoneScene();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e) {
            switch (_screenState) {
                case 0:
                    ShowCodeScene();
                    break;
                case 1:
                    ShowNameScene();
                    break;
                case 2:
                    NavigationService.Navigate(new Uri("/UI/DialogList.xaml", UriKind.Relative));
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown screen state");
                    break;
            }

            _screenState++;
        }

        private void ShowPhoneScene() {
            phoneControl.Visibility = System.Windows.Visibility.Visible;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShowCodeScene() {
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Visible;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShowNameScene() {
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
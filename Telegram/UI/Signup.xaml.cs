using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;

namespace Telegram.UI {
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
                    NavigationService.Navigate(new Uri("/UI/StartPage.xaml", UriKind.Relative));
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown screen state");
                    break;
            }

        }

        private void ShowPhoneScene() {
            phoneControl.Visibility = System.Windows.Visibility.Visible;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;

        }

        private void ShowCodeScene() {
            if (!phoneControl.FormValid()) {
                phoneControl.PhoneNumberHinTextBlock.Foreground = new SolidColorBrush(Colors.Red);

                return;
            }

            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Visible;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;

            _screenState++;
        }

        private void ShowNameScene() {
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Visible;

            _screenState++;
        }
    }
}
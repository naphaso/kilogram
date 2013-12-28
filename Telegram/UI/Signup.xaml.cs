using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Telegram.MTProto;
using Telegram.UI.Flows;

namespace Telegram.UI {
    public partial class SignupPhone : PhoneApplicationPage {
        int _screenState = 0;
        private readonly TelegramSession session;
        private readonly Login flow;

        public SignupPhone() {
            InitializeComponent();
            session = TelegramSession.Instance;
            flow = new Login(session, "en");
            
            ShowPhoneScene();

            Login();
        }

        private async Task Login() {
            await session.ConnectAsync();

            flow.NeedCodeEvent += delegate(Login login) {
                ShowCodeScene();
            };

            flow.WrongCodeEvent += delegate(Login login) {
                ShowCodeScene();
                codeControl.SetCodeInvalid();
            };

            flow.NeedSignupEvent += delegate(Login login) {
                ShowNameScene();
            };

            flow.LoginSuccessEvent += delegate(Login login) {
                NavigationService.Navigate(new Uri("/UI/StartPage.xaml", UriKind.Relative));
            };

            await flow.Start();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e) {
            switch (_screenState) {
                case 0:
                    flow.SetPhone(phoneControl.GetPhone());
//                    ShowCodeScene();
                    break;
                case 1:
                    flow.SetCode(codeControl.GetCode());
                    break;
                case 2:
                    flow.SetSignUp(nameControl.GetFirstName(), nameControl.GetLastName());
//                    NavigationService.Navigate(new Uri("/UI/StartPage.xaml", UriKind.Relative));
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown screen state");
                    break;
            }

        }

        private void ShowPhoneScene() {
            Debug.WriteLine("ShowPhoneScene");
            phoneControl.Visibility = System.Windows.Visibility.Visible;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShowCodeScene() {
            Debug.WriteLine("ShowPhoneScene");
            if (!phoneControl.FormValid()) {
                phoneControl.PhoneNumberHinTextBlock.Foreground = new SolidColorBrush(Colors.Red);

                return;
            }

            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Visible;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
            _screenState++;
        }

        private void RestartTimer() {
            
        }

        private void UpdateTimer(int seconds) {
            
        }

        private void ShowNameScene() {
            Debug.WriteLine("ShowNameScene");
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Visible;

            _screenState++;
        }
    }
}
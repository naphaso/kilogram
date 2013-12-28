using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Windows.UI.Core;
using Microsoft.Phone.Controls;
using Telegram.MTProto;
using Telegram.UI.Flows;

namespace Telegram.UI {
    public partial class SignupPhone : PhoneApplicationPage {
        int _screenState = 0;
        private readonly TelegramSession session;
        private readonly Login flow;
        private int _timerSeconds = 60;
        private Popup _progressPopup;

        public SignupPhone() {
            InitializeComponent();
            session = TelegramSession.Instance;
            flow = new Login(session, "en");
            
            ShowPhoneScene();

            Login();
        }

        private async Task Login() {
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
                HideProgress();
                NavigationService.Navigate(new Uri("/UI/StartPage.xaml", UriKind.Relative));
            };

            await flow.Start();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e) {
            switch (_screenState) {
                case 0:
                    ShowProgress();
                    flow.SetPhone(phoneControl.GetPhone());
                    break;
                case 1:
                    ShowProgress();
                    flow.SetCode(codeControl.GetCode());
                    break;
                case 2:
                    ShowProgress();
                    flow.SetSignUp(nameControl.GetFirstName(), nameControl.GetLastName());
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
            Debug.WriteLine("ShowCodeScene");

            HideProgress();

            if (!phoneControl.FormValid()) {
                phoneControl.PhoneNumberHinTextBlock.Foreground = new SolidColorBrush(Colors.Red);

                return;
            }

            RestartTimer();

            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Visible;
            nameControl.Visibility = System.Windows.Visibility.Collapsed;
            _screenState++;
        }

        private void RestartTimer() {
            _timerSeconds = 60;
            StartTimer();
        }

        private void HaltTimer() {
            _timerSeconds = 0;
        }

        private async void StartTimer() {
            while (_timerSeconds != 0) {
                _timerSeconds--;
                codeControl.SetTimerTime(_timerSeconds);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private void ShowProgress() {
            _progressPopup = new Popup();
            UserControl content = new ProgressBarUserControl();
            _progressPopup.Child = content;

            _progressPopup.HorizontalAlignment = HorizontalAlignment.Center;
            _progressPopup.VerticalAlignment = VerticalAlignment.Center;

            _progressPopup.VerticalOffset = (this.ActualHeight - content.ActualHeight) / 2;
            
            _progressPopup.IsOpen = true;
            this.IsEnabled = false;
        }

        private void HideProgress() {
            this._progressPopup.IsOpen = false;
            this.IsEnabled = true;
        }

        private void ShowNameScene() {
            Debug.WriteLine("ShowNameScene");

            HideProgress();
            
            phoneControl.Visibility = System.Windows.Visibility.Collapsed;
            codeControl.Visibility = System.Windows.Visibility.Collapsed;
            nameControl.Visibility = System.Windows.Visibility.Visible;

            HaltTimer();

            _screenState++;
        }
    }
}
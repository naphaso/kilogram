using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telegram.Core.Logging;
using Telegram.Model;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI
{
    public partial class Settings : PhoneApplicationPage
    {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Settings));

        public Settings()
        {
            InitializeComponent();

            // FIXME: hardcoded model
            if (App.SettingsModel == null) {
                App.SettingsModel = new MainSettingsModel();
                App.SettingsModel.Init();
            }

            // get data model from Telegram API settings
            this.DataContext = App.SettingsModel;
        }

        private void Edit_Click(object sender, EventArgs e) {

        }

        private void SettingsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender.GetType() != typeof (LongListSelector))
                return;

            var selector = (LongListSelector) sender;

            if (selector.SelectedItem.GetType() != typeof (MainSettingsItem))
                return;

            var item = (MainSettingsItem) selector.SelectedItem;

            if (item.Name == "notifications") {
                Debug.WriteLine("Selected notifications");
                NavigationService.Navigate(new Uri("/UI/Pages/SettingsNotification.xaml", UriKind.Relative));
            }
            else {
                Debug.WriteLine("Uknown selection");
            }
            
            
        }

        private void OnChangeAvatar(object sender, GestureEventArgs e) {
            var photo = new PhotoChooserTask { ShowCamera = true };
            photo.Completed += photoChooserTask_Completed;
            photo.Show();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e) {
            try {
                if (e.ChosenPhoto == null)
                    return;

                Task.Run(() => StartUploadPhoto(e.OriginalFileName, e.ChosenPhoto));
            } catch (Exception exception) {
                Debug.WriteLine("Exception in photoChooserTask_Completed " + exception.Message);
            }
        }

        private async Task StartUploadPhoto(string name, Stream stream) {
            try {
                Deployment.Current.Dispatcher.BeginInvoke(() => {
                    UploadProgressBar.Visibility = Visibility.Collapsed;
                });

                InputFile file =
                    await TelegramSession.Instance.Files.UploadFile(name, stream, PhotoUploadProgressHandler);
                photos_Photo photo =
                    await
                        TelegramSession.Instance.Api.photos_uploadProfilePhoto(file, "", TL.inputGeoPointEmpty(),
                            TL.inputPhotoCropAuto());

                Photos_photoConstructor photoConstructor = (Photos_photoConstructor) photo;

                foreach (var user in photoConstructor.users) {
                    TelegramSession.Instance.SaveUser(user);
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => {
                    UploadProgressBar.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        private void PhotoUploadProgressHandler(float progress) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                UploadProgressBar.Value = (double) progress;
            });
        }

        private void OnLogout(object sender, GestureEventArgs e) {
            MessageBoxResult result = MessageBox.Show("This will clear all your saved data. Continue?",
"Confirm action", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK) {
                Task.Run(() => DoLogout());
            }
        }

        private async Task DoLogout() {
            await TelegramSettings.Instance.Notifications().UnregisterPushNotifications();
            TelegramSession.Instance.clear();
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                NavigationService.Navigate(new Uri("/UI/Pages/Signup.xaml", UriKind.Relative));
            });
        }
    }
}
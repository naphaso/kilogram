using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using Windows.UI.Core;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telegram.Core.Logging;
using Telegram.Model;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.UI.Controls;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Logger = Telegram.Core.Logging.Logger;

namespace Telegram.UI {
    public partial class DialogPage : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListControl));
        private TelegramSession session;

        private bool keyboardWasShownBeforeEmojiPanelIsAppeared;

        private DialogModel model;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("modelId", out uriParam)) {
                model = TelegramSession.Instance.Dialogs.Model.Dialogs[(int.Parse(uriParam))];
            }
            else {
                logger.error("Unable to get model id from navigation");
            }

            UpdateDataContext();

            // init notice
            // FIXME: assure that no actual history received from server
            // or this is new chat
            if (MessageLongListSelector.ItemsSource == null || MessageLongListSelector.ItemsSource.Count == 0)
                ShowNotice();
        }

        protected override void OnBackKeyPress(CancelEventArgs e) {
            if (EmojiPopup.IsOpen) {
                ToggleEmoji();
                e.Cancel = true;
                return;
            }

            if (AttachPopup.IsOpen) {
                ToggleAttach();
                e.Cancel = true;
                return;
            }

            NavigationService.Navigate(new Uri("/UI/Pages/StartPage.xaml", UriKind.Relative));
        }

        private void UpdateDataContext() {
            this.DataContext = model;
            MessageLongListSelector.ItemsSource = model.Messages;
//            MessageLongListSelector.ItemRealized += delegate {
//                if (MessageLongListSelector.ItemsSource == null ||
//                    MessageLongListSelector.ItemsSource.Count == 0)
//                    return;
//
//                MessageLongListSelector.ScrollTo(
//                    MessageLongListSelector.ItemsSource[MessageLongListSelector.ItemsSource.Count - 1]);
//            };
        }

        public DialogPage() {
//            this.BackKeyPress += delegate {
//
//            };

            session = TelegramSession.Instance;

//            this.DataContext = MessageModel;

            InitializeComponent();
            DisableEditBox();

            messageEditor.GotFocus += delegate {
                AttachPopup.IsOpen = false;
                EmojiPopup.IsOpen = false;
                MainPanel.Margin = new Thickness(0, 0, 0, -20);
            };

            messageEditor.LostFocus += delegate {
                if (!EmojiPopup.IsOpen && !AttachPopup.IsOpen)
                    MainPanel.Margin = new Thickness(0, 0, 0, 0);
            };


            EmojiPanelControl.EmojiGridListSelector.SelectionChanged += EmojiGridListSelectorOnSelectionChanged;

//            dialogList.ItemsSource
        }


        private void ShowNotice() {
            if (IsPrivate()) {
                SecretChatNoticeControl.Visibility = Visibility.Visible;
            } else {
                ChatNoticeControl.Visibility = Visibility.Visible;
            }
        }

        private void HideNotice() {
            SecretChatNoticeControl.Visibility = Visibility.Collapsed;
            ChatNoticeControl.Visibility = Visibility.Collapsed;
        }

        private bool IsPrivate() {
            return false;
        }

        private void EmojiGridListSelectorOnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs) {
            Debug.WriteLine("Emoji clicked");
            EmojiItemModel emoji = (sender as LongListSelector).SelectedItem as EmojiItemModel;
            messageEditor.Text += emoji.ToString();
        }

        private void Dialog_Message_Send(object sender, EventArgs e) {
            var text = messageEditor.Text;

            messageEditor.Text = "";
            model.SendMessage(text);
            //Toaster.Show("Igor Glotov", text);
        }

        private void PickAndSendPhoto(object sender, GestureEventArgs e) {
            var photo = new PhotoChooserTask { ShowCamera = true };
            photo.Completed += photoChooserTask_Completed;
            photo.Show();
        }

        private void PickAndSendVideo(object sender, GestureEventArgs e) {
            var photo = new PhotoChooserTask {  };
            photo.Completed += photoChooserTask_Completed;
            photo.Show();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e) {
            try {
                if (e.ChosenPhoto == null)
                    return;

                Task.Run(() => StartUploadPhoto(e.OriginalFileName, e.ChosenPhoto));

                if (AttachPopup.IsOpen)
                    ToggleAttach();

            } catch (Exception exception) {
                Debug.WriteLine("Exception in photoChooserTask_Completed " + exception.Message);
            }
        }

        private async Task StartUploadPhoto(string name, Stream stream) {
            try {
//                Deployment.Current.Dispatcher.BeginInvoke(() => {
//                    UploadProgressBar.Visibility = Visibility.Collapsed;
//                });

                if (!(model is DialogModelPlain)) 
                    return;

                DialogModelPlain plainModel = (DialogModelPlain) model;

                InputFile file =
                    await TelegramSession.Instance.Files.UploadFile(name, stream, delegate { });

                InputMedia media = TL.inputMediaUploadedPhoto(file);


                Deployment.Current.Dispatcher.BeginInvoke(() => {
                    plainModel.SendMedia(media);
                });
//                Deployment.Current.Dispatcher.BeginInvoke(() => {
//                    UploadProgressBar.Visibility = Visibility.Collapsed;
//                });
            } catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        private void ToggleAttach() {
            this.Focus();
            EmojiPopup.IsOpen = false;

            AttachPopup.IsOpen = !AttachPopup.IsOpen;
            if (AttachPopup.IsOpen)
                MainPanel.Margin = new Thickness(0, 0, 0, AttachPopup.Height);
            else
                MainPanel.Margin = new Thickness(0, 0, 0, 0);  
        }

        private void Dialog_Attach(object sender, EventArgs e) {
            ToggleAttach();
        }

        private void ToggleEmoji() {
            this.Focus();

            AttachPopup.IsOpen = false;
            EmojiPopup.IsOpen = !EmojiPopup.IsOpen;
            if (EmojiPopup.IsOpen)
                MainPanel.Margin = new Thickness(0, 0, 0, EmojiPopup.Height);
            else
                MainPanel.Margin = new Thickness(0, 0, 0, 0);
        }

        private void Dialog_Emoji(object sender, EventArgs e) {
            ToggleEmoji();
        }

        private int GetEditorTotalHeight() {
            return (int) messageEditor.Height + (int) messageEditor.Margin.Top + (int) messageEditor.Margin.Bottom;
        }

        private void Dialog_Manage(object sender, EventArgs e) {
            messageEditor.Text += "\uD83C\uDFAA";
        }

        private void Dialog_Message_Change(object sender, TextChangedEventArgs e) {
            if (messageEditor.Text.Length > 0)
                EnableEditBox();
            else 
                DisableEditBox();
        }

        private void EnableEditBox() {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true; 
        }

        private void DisableEditBox() {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false; 
        }

        private void OnHeaderTap(object sender, GestureEventArgs e) {
            int modelId = TelegramSession.Instance.Dialogs.Model.Dialogs.IndexOf(model);
            NavigationService.Navigate(new Uri("/UI/Pages/UserProfile.xaml?modelId=" + modelId, UriKind.Relative));
        }

        private void OnOpenAttachment(object sender, GestureEventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/MediaViewPage.xaml", UriKind.Relative));
        }
    }
}
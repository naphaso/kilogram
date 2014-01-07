using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
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

        private void UpdateDataContext() {
            this.DataContext = model;
            MessageLongListSelector.ItemsSource = model.Messages;            
        }

        public DialogPage() {
            this.BackKeyPress += delegate {
                NavigationService.Navigate(new Uri("/UI/Pages/StartPage.xaml", UriKind.Relative));
            };

            session = TelegramSession.Instance;

//            this.DataContext = MessageModel;

            InitializeComponent();
            DisableEditBox();

            messageEditor.GotFocus += delegate {
                if (emojiPanelShowing == false)
                    keyboardWasShownBeforeEmojiPanelIsAppeared = true;
                else 
                    HideEmojiPanel();

            };
            messageEditor.LostFocus += delegate {
                if (emojiPanelShowing == false)
                    keyboardWasShownBeforeEmojiPanelIsAppeared = false;
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

        private void PickAndSendPhoto() {
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
//                Deployment.Current.Dispatcher.BeginInvoke(() => {
//                    UploadProgressBar.Visibility = Visibility.Collapsed;
//                });

                InputFile file =
                    await TelegramSession.Instance.Files.UploadFile(name, stream, delegate { });

                InputMedia media = TL.inputMediaUploadedPhoto(file);


                Deployment.Current.Dispatcher.BeginInvoke(() => {
                    model.SendMedia(media);
                });
//                Deployment.Current.Dispatcher.BeginInvoke(() => {
//                    UploadProgressBar.Visibility = Visibility.Collapsed;
//                });
            } catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        private void Dialog_Attach(object sender, EventArgs e) {
//            Toaster.Show("Igor Glotov", "Hello");

            new PhoneFlipMenu(new PhoneFlipMenuAction("photo", PickAndSendPhoto),
                    new PhoneFlipMenuAction("video", () => { MessageBox.Show("sorry, not impelmented"); }),
                    new PhoneFlipMenuAction("location", () => { MessageBox.Show("sorry, not impelmented"); }),
                    new PhoneFlipMenuAction("document", () => { MessageBox.Show("sorry, not impelmented"); }),
                    new PhoneFlipMenuAction("contact", () => { MessageBox.Show("sorry, not impelmented"); })).Show();
        }


        private bool emojiPanelShowing = false;
        private void Dialog_Emoji(object sender, EventArgs e) {
            if (emojiPanelShowing) {
                if (keyboardWasShownBeforeEmojiPanelIsAppeared)
                    messageEditor.Focus();
                HideEmojiPanel();
            }
            else {
                if (keyboardWasShownBeforeEmojiPanelIsAppeared)
                    this.Focus();
                ShowEmojiPanel();
            }
        }

        private void HideEmojiPanel() {
            MessageLongListSelector.Height = ContentPanel.Height - GetEditorTotalHeight();
            emojiPanelShowing = false;
        }

        private void ShowEmojiPanel() {
            MessageLongListSelector.Height = ContentPanel.Height - EditorEmojiPanel.Height;
            emojiPanelShowing = true;
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
    }
}
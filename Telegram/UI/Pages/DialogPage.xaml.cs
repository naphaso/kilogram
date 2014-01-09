using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using Telegram.Utils;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Logger = Telegram.Core.Logging.Logger;

namespace Telegram.UI {
    public partial class DialogPage : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListControl));
        private TelegramSession session;

        private bool keyboardWasShownBeforeEmojiPanelIsAppeared;

        private DialogModel model = null;
        private volatile bool needDialogCreate = false;
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("modelId", out uriParam)) {
                model = TelegramSession.Instance.Dialogs.Model.Dialogs[(int.Parse(uriParam))];
            } else if (NavigationContext.QueryString.TryGetValue("userId", out uriParam)) {
                int userId = int.Parse(uriParam);
                var targetPeer = TL.peerUser(userId);

                foreach (DialogModel dialogModel in TelegramSession.Instance.Dialogs.Model.Dialogs) {
                    if (dialogModel is DialogModelEncrypted)
                        continue;
                    
                    if (TLStuff.PeerEquals(dialogModel.Peer, targetPeer)) {
                        model = dialogModel;
                        break;
                    }
                }

                if (model == null) {
                    model = new DialogModelPlain(TL.peerUser(userId), TelegramSession.Instance);
                    needDialogCreate = true;
                }

                
            }

            TelegramSession.Instance.Dialogs.OpenedDialog = model;

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
            model.Messages.CollectionChanged += delegate {
                if (MessageLongListSelector.ItemsSource == null || MessageLongListSelector.ItemsSource.Count == 0)
                    ShowNotice();
                else
                    HideNotice();
            };
        }

        public DialogPage() {

            session = TelegramSession.Instance;

            InitializeComponent();
            DisableEditBox();

            messageEditor.GotFocus += delegate {
                AttachPopup.IsOpen = false;
                EmojiPopup.IsOpen = false;
            };

            messageEditor.LostFocus += delegate {
                if (!EmojiPopup.IsOpen && !AttachPopup.IsOpen)
                    MainPanel.Margin = new Thickness(0, 0, 0, 0);
            };

            EmojiPanelControl.BackspaceClick += EmojiPanelControlOnBackspaceClick;
            EmojiPanelControl.KeyboardClick += EmojiPanelControlOnKeyboardClick;
            EmojiPanelControl.EmojiGridListSelector.SelectionChanged += EmojiGridListSelectorOnSelectionChanged;

        }

        private void EmojiPanelControlOnKeyboardClick(object sender, object args) {
            if (EmojiPopup.IsOpen)
                ToggleEmoji();

            messageEditor.Focus();
        }

        private void EmojiPanelControlOnBackspaceClick(object sender, object args) {
            if (messageEditor.Text.Length == 0)
                return;

            var utf32list = messageEditor.Text.ToUtf32().ToList();
            utf32list.RemoveAt(utf32list.Count-1);
            messageEditor.Text = new string(utf32list.ToUtf16().ToArray());
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
            var selector = (LongListSelector) sender;
            if (selector.SelectedItem == null)
                return;
            var emoji = (EmojiItemModel)selector.SelectedItem;
            messageEditor.Text += emoji.ToString();
            selector.SelectedItem = null;
        }

        private void Dialog_Message_Send(object sender, EventArgs e) {
            var text = messageEditor.Text;
            messageEditor.Text = "";

            if (needDialogCreate) {
                model.SendMessage(text).ContinueWith((result) => {
                    if (result.Result) {
                        TelegramSession.Instance.Dialogs.Model.Dialogs.Add(model);
                        needDialogCreate = false;
                    }
                });
            } else { 
                model.SendMessage(text);
            }
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
            if (EmojiPopup.IsOpen) {
                MainPanel.Margin = new Thickness(0, 0, 0, EmojiPopup.Height);
                EmojiPanelControl.Show();
            } else
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
            Peer peer = model.Peer;
            if (peer.Constructor == Constructor.peerUser) {
                NavigationService.Navigate(new Uri("/UI/Pages/UserProfile.xaml?userId=" + ((PeerUserConstructor) peer).user_id, UriKind.Relative));
            } else {
                NavigationService.Navigate(new Uri("/UI/Pages/ChatSettings.xaml?chatId=" + ((PeerChatConstructor)peer).chat_id, UriKind.Relative));

            }
        }

        private void OnOpenAttachment(object sender, GestureEventArgs e) {
            var element = (FrameworkElement)sender;
            MessageModel message = (MessageModel)element.DataContext;

            NavigationService.Navigate(new Uri("/UI/Pages/MediaViewPage.xaml", UriKind.Relative));
        }

        private void UserAvatarTap(object sender, GestureEventArgs e) {
            var element = (FrameworkElement)sender;
            MessageModel message = (MessageModel) element.DataContext;

            NavigationService.Navigate(new Uri("/UI/Pages/UserProfile.xaml?userId=" + message.Sender.Id, UriKind.Relative));
        }

        private void OnForwardedTap(object sender, GestureEventArgs e) {
            var element = (FrameworkElement)sender;
            MessageModel message = (MessageModel)element.DataContext;
            
            NavigationService.Navigate(new Uri("/UI/Pages/UserProfile.xaml?userId=" + message.ForwardedId, UriKind.Relative));
        }

        private void OnMessageContextMenuOpened(object sender, RoutedEventArgs e) {
            
        }

        private void OnDeleteMessage(object sender, RoutedEventArgs e) {
            var message = ((sender as MenuItem).DataContext as MessageModel);

            DoDeleteMessage(message);
        }

        private async Task DoDeleteMessage(MessageModel message) {
            List<int> idsDeleted = await TelegramSession.Instance.Api.messages_deleteMessages(new List<int>(message.Id));

            if (idsDeleted.Count > 0) {
                model.Messages.Remove(message);
            }
        }

        private void OnForwardMessage(object sender, RoutedEventArgs e) {
            var message = ((sender as MenuItem).DataContext as MessageModel);
            
            if (message.Id == 0)
                return;

            NavigationService.Navigate(new Uri("/UI/Pages/DialogListForwarding.xaml?messageId=" + message.Id, UriKind.Relative));
        }

        private void OnCopyMessage(object sender, RoutedEventArgs e) {
            var message = ((sender as MenuItem).DataContext as MessageModel);
            Clipboard.SetText(message.Text);
        }
    }
}
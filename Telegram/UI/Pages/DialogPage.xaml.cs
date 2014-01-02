using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model;

namespace Telegram.UI {
    public partial class DialogPage : PhoneApplicationPage {
        public static DialogMessageModel MessageModel = null; // FIXME testing purpose only
        private bool keyboardWasShownBeforeEmojiPanelIsAppeared;

        public DialogPage() {
            if (MessageModel == null)
                MessageModel = new DialogMessageModel();

            MessageModel.Init();

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

            // init notice
            // FIXME: assure that no actual history received from server
            // or this is new chat
            if (dialogList.ItemsSource == null || dialogList.ItemsSource.Count == 0)
                ShowNotice();

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
            var dialogMessageItem = new DialogMessageItem() { Sender = "editor", Text = text, Time = "14:88", IsOut = true};
            MessageModel.Items.Add(dialogMessageItem);
            messageEditor.Text = "";
            dialogList.ScrollTo(dialogMessageItem);

            Toaster.Show("Igor Glotov", text);
        }

        private void Dialog_Attach(object sender, EventArgs e) {
            Toaster.Show("Igor Glotov", "Hello");
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
            dialogList.Height = ContentPanel.Height - GetEditorTotalHeight();
            emojiPanelShowing = false;
        }

        private void ShowEmojiPanel() {
            dialogList.Height = ContentPanel.Height - EditorEmojiPanel.Height;
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
    }
}
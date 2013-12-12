using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model;

namespace Telegram.UI {
    public partial class Dialog : PhoneApplicationPage {
        public static DialogMessageModel MessageModel = null; // FIXME testing purpose only

        public Dialog() {
            if (MessageModel == null)
                MessageModel = new DialogMessageModel();

            MessageModel.Init();

            this.DataContext = MessageModel;

            InitializeComponent();
            DisableEditBox();
        }

        private void Dialog_Message_Send(object sender, EventArgs e) {
            var text = messageEditor.Text;
            var dialogMessageItem = new DialogMessageItem() { Sender = "editor", Text = text, Time = "14:88", IsOut = true};
            MessageModel.Items.Add(dialogMessageItem);
            messageEditor.Text = "";
            dialogList.ScrollTo(dialogMessageItem);
        }

        private void Dialog_Attach(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Dialog_Emoji(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Dialog_Manage(object sender, EventArgs e) {
            throw new NotImplementedException();
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
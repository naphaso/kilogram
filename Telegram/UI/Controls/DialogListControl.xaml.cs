using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.UI.Models;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Telegram.UI.Controls {
    public partial class DialogListControl : UserControl {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListControl));

        public event OnDialogSelected DialogSelected;

        protected virtual void OnSelected(DialogModel model) {
            OnDialogSelected handler = DialogSelected;
            if (handler != null) handler(this, model);
        }

        public DialogListControl() {
            InitializeComponent();

            LoadModel();
            DialogList.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
                var longListSelector = sender as LongListSelector;
                if (longListSelector == null) {
                    logger.error("sender as LongListSelector == null");
                    return;
                }

                var selectedDialog = longListSelector.SelectedItem as DialogModel;
                Debug.Assert(selectedDialog != null, "selectedDialog != null");

                OnSelected(selectedDialog);
            };
        }

        private void LoadModel() {
            DialogList.ItemsSource = TelegramSession.Instance.Dialogs.Model.Dialogs;
//            initDemo();
        }

        private void initDemo() {
            var items = new ObservableCollection<DialogItem> {
                new DialogItem() {Avatar = "2", Preview = "Hi there!", Timestamp = "9:56a", Title = "Jane Doe"},
                new DialogItem() {
                    Avatar = "3",
                    Preview = "Stay awhile and listen.",
                    Timestamp = "1:21a",
                    Title = "Decard Kain"
                }
            };
            items.Add(new DialogItem() {Avatar = "1", Preview = "Hello.", Timestamp = "11:21a", Title = "John Doe"});

            DialogList.ItemsSource = items;

            items.Add(new DialogItem() {
                Avatar = "4",
                Preview = "Stay awhile and listen.",
                Timestamp = "1:21a",
                Title = "Decard Kain"
            });
            DialogList.ItemsSource = new ObservableCollection<DialogItem>(items.OrderBy(i => i.Timestamp));

        }

        private void OnClearChatHistory(object sender, RoutedEventArgs e) {
            

        }

        private void OnClearAndExit(object sender, RoutedEventArgs e) {
            

        }

        private void ChatContextMenuOpened(object sender, RoutedEventArgs e) {
            

        }

        private void OnDeleteDialog(object sender, RoutedEventArgs e) {
            

        }

        private void DialogContextMenuOpened(object sender, RoutedEventArgs e) {
            

        }

        private void OnItemHold(object sender, GestureEventArgs e) {
            Debug.WriteLine("Menu item hold");
        }
    }

    public delegate void OnDialogSelected(object sender, DialogModel model);
}

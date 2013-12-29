using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.UI.Models;

namespace Telegram.UI.Controls {
    public partial class DialogListControl : UserControl {
        public DialogListControl() {
            InitializeComponent();

            initDemo();
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
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.UI.Models;

namespace Telegram.UI
{
    public partial class DialogList : PhoneApplicationPage
    {
        private static DialogsModel _dialogs = null;

        public static DialogsModel Dialogs {
            get { return _dialogs ?? (_dialogs = new DialogsModel()); }
        }

        public DialogList()
        {
            InitializeComponent();
            var items = new ObservableCollection<DialogItem> {
                new DialogItem() {Avatar = "1", Preview = "Hello.", Timestamp = "11:21a", Title = "John Doe"},
                new DialogItem() {Avatar = "2", Preview = "Hi there!", Timestamp = "9:56a", Title = "Jane Doe"},
                new DialogItem() {Avatar = "3", Preview = "Stay awhile and listen.", Timestamp = "1:21a", Title = "Decard Kain"}
            };

            dialogList.ItemsSource = items;

            items.Add(new DialogItem() { Avatar = "4", Preview = "Stay awhile and listen.", Timestamp = "1:21a", Title = "Decard Kain" });
            dialogList.ItemsSource = new ObservableCollection<DialogItem>(items.OrderBy(i => i.Timestamp));

            BuildAppBar();
        }

        private void BuildAppBar() {
            ApplicationBar = new ApplicationBar();
            
            var newButton = new ApplicationBarIconButton(new Uri("/Assets/UI/appbar.new.png", UriKind.Relative));
            newButton.Text = "Create";

            var searchButton = new ApplicationBarIconButton(new Uri("/Assets/UI/appbar.feature.search.png", UriKind.Relative));
            searchButton.Text = "Search";

            ApplicationBar.Buttons.Add(newButton);
            ApplicationBar.Buttons.Add(searchButton);

            ApplicationBar.MenuItems.Add(new ApplicationBarMenuItem("Settings"));
        }
    }
}
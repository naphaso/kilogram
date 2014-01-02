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
using Telegram.Core.Logging;
using Telegram.Model;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.UI {
    public partial class UserProfile : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(UserProfile));

        private List<GalleryItemModel> items;
        private UserModel model;

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("modelId", out uriParam)) {
                DialogModel dialogModel = TelegramSession.Instance.Dialogs.Model.Dialogs[(int.Parse(uriParam))];
                switch (dialogModel.Peer.Constructor) {
                    case Constructor.peerChat:
                        logger.error("Navigation error: page is UserProfile but peer is CHAT");
                        break;
                    case Constructor.peerUser:
                        var peerUser = dialogModel.Peer as PeerUserConstructor;
                        model = TelegramSession.Instance.GetUser(peerUser.user_id);
                        break;
                }
            } else {
                logger.error("Unable to get model id from navigation");
            }

            UpdateDataContext();
        }

        private void UpdateDataContext() {
            this.DataContext = model;
        }

        public UserProfile() {
            InitializeComponent();
        }

        private void Share_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Edit_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/Pages/EditUserProfile.xaml", UriKind.Relative));
        }

        private void Block_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e) {
            Debug.WriteLine("Gallery item loaded.");
        }

        private void Pivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (UserProfilePivot.SelectedIndex == 1)
                LoadGallery();
        }

        private void LoadGallery() {
            items = new List<GalleryItemModel>() {
                new GalleryItemModel() {IsVideo = false, Thumb = "/Assets/UI/placeholder.group.orange-720p.png"},
                new GalleryItemModel() {IsVideo = false, Thumb = "/Assets/UI/placeholder.group.orange-720p.png"},
                new GalleryItemModel() {IsVideo = false, Thumb = "/Assets/UI/placeholder.group.orange-720p.png"},
                new GalleryItemModel() {IsVideo = true, VideoLength = "1:32", Thumb = "/Assets/UI/placeholder.group.orange-720p.png"}
            };

            GalleryListSelector.ItemsSource = items;
        }
    
    }
}
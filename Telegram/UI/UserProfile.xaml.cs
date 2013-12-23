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
    public partial class UserProfile : PhoneApplicationPage {
        private List<GalleryItemModel> items;
        public UserProfile() {
            InitializeComponent();
        }

        private void Share_Click(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Edit_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/UI/EditUserProfile.xaml", UriKind.Relative));
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
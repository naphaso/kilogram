using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telegram.MTProto;

namespace Telegram
{
    public partial class SignupName_Control : UserControl {

        private volatile PhotoResult avatarSource = null;
        
        public SignupName_Control()
        {
            InitializeComponent();
        }

        public PhotoResult GetAvatarSource() {
            return avatarSource;
        }

        private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var photo = new PhotoChooserTask {ShowCamera = true, PixelHeight = 200, PixelWidth = 200};
            photo.Completed += photoChooserTask_Completed;
            photo.Show();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e) {
            try {
                if (e.ChosenPhoto == null)
                    return;

                var bi = new BitmapImage();
                bi.SetSource(e.ChosenPhoto);
                avatarSource = e;
                AvatarHandle.SetImage(bi);

//                TelegramSession.Instance.
            }
            catch (Exception exception) {
                Debug.WriteLine("Exception in photoChooserTask_Completed " + exception.Message);
            }
        }

        public string GetFirstName() {
            return FirstNameTextBox.Text;
        }

        public string GetLastName() {
            return LastNameTextBox.Text;
        }

    }
}

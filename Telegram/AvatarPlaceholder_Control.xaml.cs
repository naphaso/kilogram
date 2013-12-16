using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram
{
    public partial class AvatarPlaceholder_Control : UserControl
    {
        public AvatarPlaceholder_Control()
        {
            InitializeComponent();
        }

        public void SetImage(ImageSource src) {
            AvatarImage.Source = src;
            AvatarImage.Visibility = Visibility.Visible;
        }
    }
}

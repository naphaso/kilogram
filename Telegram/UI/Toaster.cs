using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Shell;

namespace Telegram.UI {
    class Toaster {
        public static void Show(string sender, string message) {
            var toast = GetToastWithImgAndTitle(sender, message);
            toast.TextWrapping = TextWrapping.Wrap;

            toast.Show();
//            ShellToast toast = new ShellToast();
//            toast.Content = "This is a local toast";
//            toast.Title = "MYAPP";
//            toast.Show();
        }

        private static ToastPrompt GetToastWithImgAndTitle(string header, string content) {
            return new ToastPrompt {
                Title = header,                
                Message = content,
//                ImageHeight = 50,
//                ImageWidth = 50,
//                ImageSource = new BitmapImage(new Uri("/Assets/UI/placeholder.user.green-WVGA.png", UriKind.RelativeOrAbsolute))
            };
        }
    }
}

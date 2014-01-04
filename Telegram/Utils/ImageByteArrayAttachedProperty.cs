using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Telegram.Utils {
    public class ImageByteArrayAttachedProperty {

        public static readonly DependencyProperty ByteArraySourceProperty =
            DependencyProperty.RegisterAttached("ByteArraySource", typeof (Byte[]),
                typeof (ImageByteArrayAttachedProperty), new PropertyMetadata(default(Byte[]), byteArraySource));

        private static void byteArraySource(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Image img = d as Image;
            if (e.NewValue != null)
            {
                img.Source = ByteArraytoBitmap((Byte[]) e.NewValue);
            }
        }

    public static BitmapImage ByteArraytoBitmap(Byte[] byteArray)
    {
        MemoryStream stream = new MemoryStream(byteArray);
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.SetSource(stream);
        return bitmapImage;
    }

    public static void SetByteArraySource(UIElement element, Byte[] value)
    {
        element.SetValue(ByteArraySourceProperty, value);
    }

    public static Byte[] GetByteArraySource(UIElement element)
    {
        return (Byte[]) element.GetValue(ByteArraySourceProperty);
    }
}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using System.Windows.Media;

namespace Telegram.UI.Converters {
        public class UserOnlineStatusColorConverter : IValueConverter {
            private static readonly Logger logger = LoggerFactory.getLogger(typeof(UserOnlineStatusColorConverter));

            #region IValueConverter Members


            public object Convert(object value, Type targetType,
                object parameter, System.Globalization.CultureInfo culture) {

                string status = (string) value;
                Color color;
                if (status == "online") {
                    color = (Color)Application.Current.Resources["PhoneAccentColor"];
                } else {
                    color = Color.FromArgb(255, 153, 153, 153);
                }

                return new SolidColorBrush(color);
            }

            public object ConvertBack(object value, Type targetType,
                object parameter, System.Globalization.CultureInfo culture) {
                return "";
            }

            #endregion
        }
    
}

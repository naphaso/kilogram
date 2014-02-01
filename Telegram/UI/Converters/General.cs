using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.Utils;

namespace Telegram.UI.Converters {
    public class BoolToVisibilityConverter : IValueConverter {

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            bool val = (bool) value;

            if (val == true)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            return false;
        }

        #endregion
    }

    public class PhoneStringConverter : IValueConverter
    {

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            string number = (string)value;
            return Formatters.FormatPhoneNumberWithCulture(number, culture);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}

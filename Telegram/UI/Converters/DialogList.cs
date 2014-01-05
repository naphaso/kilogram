using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Converters {
    public class DialogList {

    }

    public class DialogStatusToStringConverter : IValueConverter {

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            DialogModel.DialogStatus status = (DialogModel.DialogStatus)value;
            return status.String;

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class DialogStatusToColorConverter : IValueConverter {

        #region IValueConverter Members
        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            DialogModel.DialogStatus status = (DialogModel.DialogStatus)value;
            Color color;
            if (status.Type == DialogModel.StatusType.Activity) {
                color = (Color) Application.Current.Resources["PhoneAccentColor"];
            }
            else {
                color = Color.FromArgb(255, 153, 153, 153);
            }

            return color;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}

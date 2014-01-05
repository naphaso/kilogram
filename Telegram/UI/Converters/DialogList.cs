using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Converters {
    public class DialogList {

    }

    public class DialogStatusToStringConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogStatusToStringConverter));

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            DialogModel.DialogStatus status = (DialogModel.DialogStatus)value;
            logger.debug("converter string is {0}", status.String);
            return status.String;

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class DialogStatusToColorConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogStatusToColorConverter));

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

            logger.debug("converter color is {0}", color);

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}

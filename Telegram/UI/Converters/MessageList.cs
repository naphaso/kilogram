using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Telegram.Annotations;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Converters {

    public class MessageDeliveryStateConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MessageDeliveryStateConverter));

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            MessageModel.MessageDeliveryState status = (MessageModel.MessageDeliveryState)value;

            string itemPath = "";
            switch (status) {
                case MessageModel.MessageDeliveryState.Delivered:
                    itemPath = "/Assets/UI/message.state.sent-WVGA.png";
                    break;

                case MessageModel.MessageDeliveryState.Pending:
                    itemPath = "/Assets/UI/message.state.sending-WVGA.png";
                    break;

                case MessageModel.MessageDeliveryState.Read:
                    itemPath = "/Assets/UI/message.state.read-WVGA.png";
                    break;
            }

            return itemPath;

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            return "";
        }

        #endregion
    }

    public class MessageAttachmentSourceConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MessageAttachmentSourceConverter));

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            if (value == null) {
                logger.debug("no attachment image detected");
                return "";
            }

            return (BitmapImage)value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            return "";
        }

        #endregion
    }

    public class MessageAttachmentPreviewVisibilityConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MessageAttachmentPreviewVisibilityConverter));

        #region IValueConverter Members


        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {

            if (value == null) {
                logger.debug("no attachment image detected, invisible");
                return Visibility.Collapsed;
            }
            logger.debug("attachment image detected, visible");
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture) {
            return "";
        }

        #endregion
    }
}

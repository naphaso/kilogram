using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Telegram.Annotations;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Converters {

    public class MessageDeliveryStateConverter : IValueConverter {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogStatusToStringConverter));

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
}

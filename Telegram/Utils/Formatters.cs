using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Telegram.Model;

namespace Telegram.Utils {
    class Formatters {
        public static string FormatPhoneNumber(string phone) {
            return phone;
        }

        public static string FormatDialogDateTimestampUnix(int seconds) {
            return FormatDialogDateTimestamp(DateTimeExtensions.DateTimeFromUnixTimestampSeconds(seconds));
        }
        public static string FormatDialogDateTimestamp(DateTime time) {
            if (DateTime.Now - time > DateTime.Now - DateTime.Today)
                return time.ToString("d MMM");

            return time.ToString("h:mmt");
        }
    }
}

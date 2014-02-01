using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using PhoneNumbers;
using Telegram.Model;

namespace Telegram.Utils {
    class Formatters {
        public static string FormatPhoneNumberWithCulture(string phone, CultureInfo culture) {
            try {
                PhoneNumber pn = PhoneNumberUtil.GetInstance().Parse(phone, culture.TwoLetterISOLanguageName.ToUpper());
                return PhoneNumberUtil.GetInstance().Format(pn, PhoneNumberFormat.INTERNATIONAL);
            }
            catch (Exception ex) {
                return phone;
            }
        }
        
        public static string FormatPhoneNumber(string phone) {
            try {
                PhoneNumber pn = PhoneNumberUtil.GetInstance().ParseAndKeepRawInput(phone, "RU");
                return PhoneNumberUtil.GetInstance().Format(pn, PhoneNumberFormat.INTERNATIONAL);
            }
            catch (Exception) {
                return phone;
            }
        }

        public static string FormatDialogDateTimestampUnix(int seconds) {
            return FormatDialogDateTimestamp(DateTimeExtensions.DateTimeFromUnixTimestampSeconds(seconds));
        }
        public static string FormatDialogDateTimestamp(DateTime time) {
            if (DateTime.Now - time > DateTime.Now - DateTime.Today)
                return time.ToString("d MMM");

            return time.ToString("h:mmt");
        }

        public static string FormatDayWasOnlineUnix(int seconds) {
            return FormatDayWasOnline(DateTimeExtensions.DateTimeFromUnixTimestampSeconds(seconds));
        }

        public static string FormatTimeWasOnlineUnix(int seconds) {
            return FormatTimeWasOnline(DateTimeExtensions.DateTimeFromUnixTimestampSeconds(seconds));
        }

        public static string FormatDayWasOnline(DateTime time) {
            if (DateTime.Now - time > DateTime.Now - DateTime.Today)
                return time.ToString("d MMM");

            return "today";
        }

        public static string FormatTimeWasOnline(DateTime time) {
            return time.ToString("h:mmt");
        }
    }
}

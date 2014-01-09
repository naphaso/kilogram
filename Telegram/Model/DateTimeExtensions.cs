using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Model {
    class DateTimeExtensions {
        private static readonly DateTime UnixEpoch =
    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
        

        public static long GetCurrentUnixTimestampMillis() {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        public static long GetUnixTimestampSeconds(DateTime time) {
            return (long) (time - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestampMillis(long millis) {
            return UnixEpoch.AddMilliseconds(millis);
        }

        public static long GetCurrentUnixTimestampSeconds() {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestampSeconds(long seconds) {
            return UnixEpoch.AddSeconds(seconds);
        }
    }
}

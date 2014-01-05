using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Telegram.Utils {
    class Helpers {
        private static Random random = new Random();

        public static ulong GenerateRandomUlong() {
            ulong rand = (((ulong)random.Next()) << 32) | ((ulong)random.Next());
            return rand;
        }

        public static long GenerateRandomLong() {
            long rand = (((long)random.Next()) << 32) | ((long)random.Next());
            return rand;
        }

        public static BitmapImage GetBitmapImageInternal(string avatarPath) {
            BitmapImage bi = new BitmapImage();
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication()) {
                using (var stream = iso.OpenFile(avatarPath, FileMode.Open, FileAccess.Read)) {
                    bi.SetSource(stream);
                }
            }
            return bi;
        }
    }
}

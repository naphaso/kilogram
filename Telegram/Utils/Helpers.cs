using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}

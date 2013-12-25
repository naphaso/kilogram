using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Model {

    public class EmojiItemModel {
        public string Path { get; set; }
        public ulong Code { private get; set; }

        public override string ToString() {
            string result = "";

            if (Code == 0) {
                Debug.WriteLine("Emoji converter error: no code assigned");
                return result;
            }

            ulong code = Code;
            char unichar = (char) code;

            while (unichar != 0 && code != 0) {
                result = unichar + result;
                code = code >> 16;
                unichar = (char) code;
            }

            return result;
        }

    }
}

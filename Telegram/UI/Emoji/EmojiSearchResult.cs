using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.UI.Emoji {

    class EmojiSearchResult {
        public long code;
        public int start;
        public int end;

        public EmojiSearchResult(long code, int start, int end) {
            this.code = code;
            this.start = start;
            this.end = end;
        }
    }
}

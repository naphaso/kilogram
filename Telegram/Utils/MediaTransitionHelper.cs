using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.MTProto;

namespace Telegram.Utils {
    public class MediaTransitionHelper {
        private static volatile MediaTransitionHelper instance;

        public static MediaTransitionHelper Instance {
            get { return instance ?? (instance = new MediaTransitionHelper()); }
        }

        public MessageMedia Media { get; set; }
    }
}

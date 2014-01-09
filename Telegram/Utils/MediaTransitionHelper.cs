using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.Utils {
    public class MediaTransitionHelper {
        private static volatile MediaTransitionHelper instance;

        public static MediaTransitionHelper Instance {
            get { return instance ?? (instance = new MediaTransitionHelper()); }
        }


        public UserModel From { get; set; }
        public MessageMedia Media { get; set; }
    }
}

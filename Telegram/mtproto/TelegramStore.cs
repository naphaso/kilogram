using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto {
    class TelegramStore {
        private static readonly TelegramStore instance = new TelegramStore();

        static TelegramStore() {
            
        }

        private TelegramStore() {
            
        }

        public static TelegramStore Instance {
            get {
                return instance;
            }
        }


    }
}

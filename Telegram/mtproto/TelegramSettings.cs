using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto {
    class TelegramSettings {
        private static readonly TelegramSettings instance = new TelegramSettings();
        IsolatedStorageSettings settings;
        static TelegramSettings() {
            
        }

        private TelegramSettings() {
            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        public static TelegramSettings Instance {
            get {
                return instance;
            }
        }

        public long TimeOffset {
            get {
                if (settings.Contains("TimeOffset")) {
                    return (long) settings["TimeOffset"];
                } else {
                    return 0;
                }
            }

            set {
                settings["TimeOffset"] = value;
                settings.Save();
            }
        }
    }
}

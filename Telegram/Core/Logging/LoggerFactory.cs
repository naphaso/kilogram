using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Core.Logging {
    class LoggerFactory {
        public static Logger getLogger(Type type) {
            return new Logger(type);
        }

    }
}

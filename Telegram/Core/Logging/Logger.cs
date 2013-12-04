using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Core.Logging {

    enum LoggingLevel : int {
        DEBUG = 1,
        INFO = 2,
        WARNING = 3,
        ERROR = 4
    }

    class Logger {
        private const LoggingLevel level = LoggingLevel.DEBUG;

        private string typeName;
        public Logger(Type type) {
            this.typeName = type.FullName;
        }

        private void logWithLevel(LoggingLevel targetLevel, string format, params object[] args) {
            if (targetLevel >= level) {
                string outFormat = String.Format("[{0}] {1} {2}: {3}", Thread.CurrentThread.GetHashCode(), targetLevel, typeName, format);
                Debug.WriteLine(outFormat, args);
            }
        }

        public void debug(string format, params object[] args) {
            logWithLevel(LoggingLevel.DEBUG, format, args);
        }

        public void info(string format, params object[] args) {
            logWithLevel(LoggingLevel.INFO, format, args);
        }

        public void warning(string format, params object[] args) {
            logWithLevel(LoggingLevel.WARNING, format, args);
        }

        public void error(string format, params object[] args) {
            logWithLevel(LoggingLevel.ERROR, format, args);
        }
    }
}

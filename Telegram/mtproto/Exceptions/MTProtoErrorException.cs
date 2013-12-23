using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto.Exceptions {
    class MTProtoErrorException : Exception {
        private readonly int errorCode;
        private readonly string errorMessage;
        public MTProtoErrorException(int errorCode, string errorMessage) {
            this.errorCode = errorCode;
            this.errorMessage = errorMessage;
        }

        public int ErrorCode {
            get { return errorCode; }
        }

        public string ErrorMessage {
            get { return errorMessage; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto.Exceptions {
    public class MTProtoException : Exception {
        public MTProtoException() {
        }
    }

    public class MTProtoInitException : MTProtoException {
        
    }

    public class MTProtoBadMessageException : MTProtoException {
        private int errorCode;

        public MTProtoBadMessageException(int errorCode) {
            this.errorCode = errorCode;
        }

        public int ErrorCode {
            get { return errorCode; }
        }
    }
}

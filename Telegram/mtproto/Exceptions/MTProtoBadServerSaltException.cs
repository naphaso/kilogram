using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto.Exceptions {
    class MTProtoBadServerSaltException : MTProtoException {
        public ulong Salt { get; private set; }

        public MTProtoBadServerSaltException(ulong salt) {
            Salt = salt;
        }
    }
}

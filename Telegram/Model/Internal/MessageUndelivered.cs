using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.MTProto;

namespace Telegram.Model.Internal {
    class MessageUndelivered :Message {
        public override Constructor Constructor {
            get { throw new NotImplementedException(); }
        }

        public override void Write(BinaryWriter writer) {
            throw new NotImplementedException();
        }

        public override void Read(BinaryReader reader) {
            throw new NotImplementedException();
        }
    }
}

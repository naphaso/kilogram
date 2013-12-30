using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.MTProto {
    class TLStuff {
        public static InputPeer PeerToInputPeer(Peer peer) {
            switch(peer.Constructor) {
                case Constructor.peerUser: // TODO: foreign and self cases
                    return TL.inputPeerContact(((PeerUserConstructor) peer).user_id);
                case Constructor.peerChat:
                    return TL.inputPeerChat(((PeerChatConstructor) peer).chat_id);
            }
        }
    }
}

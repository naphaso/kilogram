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
                default:
                    throw new Exception("invalid constructor");
            }
        }

        public static bool PeerEquals(Peer a, Peer b) {
            if(a.Constructor != b.Constructor) {
                return false;
            }

            if(a.Constructor == Constructor.peerUser) {
                return ((PeerUserConstructor) a).user_id == ((PeerUserConstructor) b).user_id;
            }

            if(a.Constructor == Constructor.peerChat) {
                return ((PeerChatConstructor) a).chat_id == ((PeerChatConstructor) b).chat_id;
            }

            return false;
        }
    }
}

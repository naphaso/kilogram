using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.TLWrappers {
    public class MessageModel {
        private Message message;

        public MessageModel(Message message) {
            this.message = message;
        }

        public int Id {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).id;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).id;
                    case Constructor.messageService:
                        return ((MessageServiceConstructor)message).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public Message RawMessage {
            get {
                return message;
            }
        }
    }
}

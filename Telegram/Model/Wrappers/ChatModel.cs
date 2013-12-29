using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class ChatModel {
        private Chat chat;

        public ChatModel(Chat chat) {
            this.chat = chat;
        }

        public int Id {
            get {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        return ((ChatEmptyConstructor) chat).id;
                    case Constructor.chat:
                        return ((ChatConstructor) chat).id;
                    case Constructor.chatForbidden:
                        return ((ChatForbiddenConstructor) chat).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public Chat RawChat {
            get {
                return chat;
            }
        }
    }
}

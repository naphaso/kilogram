using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public delegate void ChatModelChangeHandler();
    public class ChatModel {
        private Chat chat;
        public event ChatModelChangeHandler ChangeEvent;
        public ChatModel(Chat chat) {
            this.chat = chat;
        }

        public void SetChat(Chat chat) {
            this.chat = chat;
            ChangeEvent();
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

        public string Title {
            get {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        return "empty";
                    case Constructor.chat:
                        return ((ChatConstructor)chat).title;
                    case Constructor.chatForbidden:
                        return ((ChatForbiddenConstructor)chat).title;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }


    }
}

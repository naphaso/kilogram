using System;
using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
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

        public string Text {
            get {
                switch (message.Constructor) {
                    case Constructor.message:
                        return ((MessageConstructor)message).message;
                    case Constructor.messageForwarded:
                        return ((MessageForwardedConstructor)message).message;
                    case Constructor.messageService:
                        return "service";
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public int UnixSecondsTime {
            get {
                int unixSeconds = 0;
                switch (message.Constructor) {
                    case Constructor.message:
                        unixSeconds = ((MessageConstructor)message).date;
                        break;
                    case Constructor.messageForwarded:
                        unixSeconds = ((MessageForwardedConstructor)message).date;
                        break;
                    case Constructor.messageService:
                        unixSeconds = ((MessageServiceConstructor)message).date;
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                return unixSeconds;
            }
        }

        public string TimeOrDate {
            get {
                int unixSeconds = UnixSecondsTime;

                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                dateTime = dateTime.AddSeconds(unixSeconds);

                // Now - DateTime.FromToday
                if (DateTime.Now - dateTime > TimeSpan.FromDays(1)) {
                    return dateTime.ToShortDateString();
                }
                else {
                    return dateTime.ToShortTimeString();
                }
            }
        }

        public void Write(BinaryWriter writer) {
            message.Write(writer);
        }
    }
}

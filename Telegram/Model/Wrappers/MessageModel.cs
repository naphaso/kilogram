using System;
using System.IO;
using System.Windows;
using Microsoft.Phone.Media;
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

        public string Timestamp {
            get {
                return DateTimeExtensions.DateTimeFromUnixTimestampSeconds(UnixSecondsTime).ToShortDateString();
            }
        }

        public Visibility SenderVisibility {
            get {
                return Visibility.Collapsed;
            }
        }

        public Visibility ForwardedVisibility {
            get {
                return Visibility.Collapsed;
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

        public Peer Peer {
            get {
                return null;
//                                switch (message.Constructor) {
//                    case Constructor.message: {
//                        MessageConstructor msg = (MessageConstructor) message;
//                        return msg.to_id.Constructor == Constructor.peerChat ? msg.to_id : ()
//                        ;
//                    } 
//                        break;
//                    case Constructor.messageForwarded:
//                        unixSeconds = ((MessageForwardedConstructor)message).date;
//                        break;
//                    case Constructor.messageService:
//                        unixSeconds = ((MessageServiceConstructor)message).date;
//                        break;
//                    default:
//                        throw new InvalidDataException("invalid constructor");
//                }
            }
        }

        private static Peer GetPeer(MessageConstructor message) {
            return null;
        }

        //private static Peer 

        public void Write(BinaryWriter writer) {
            message.Write(writer);
        }
    }
}

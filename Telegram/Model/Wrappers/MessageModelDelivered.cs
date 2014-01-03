using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class MessageModelDelivered : MessageModel {
        private Message message;

        public MessageModelDelivered(Message message) {
            this.message = message;
        }

        public MessageModelDelivered() {
            
        }

        public MessageModelDelivered(BinaryReader reader) {
            Read(reader);
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

        public override bool Delivered {
            get {
                return true;
            }
        }

        public override string Text {
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
            set { }
        }

        public override DateTime Timestamp {
            get {
                return DateTimeExtensions.DateTimeFromUnixTimestampSeconds(UnixSecondsTime);
            }
            set { }
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

        public Peer Peer {
            get {
                if (message.Constructor == Constructor.message) {
                    MessageConstructor msg = (MessageConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else if (message.Constructor == Constructor.messageForwarded) {
                    MessageForwardedConstructor msg = (MessageForwardedConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else if (message.Constructor == Constructor.messageService) {
                    MessageForwardedConstructor msg = (MessageForwardedConstructor)message;
                    if (msg.to_id.Constructor == Constructor.peerChat) {
                        return msg.to_id;
                    } else {
                        if (msg.output) {
                            return msg.to_id;
                        } else {
                            return TL.peerUser(msg.from_id);
                        }
                    }
                } else {
                    throw new InvalidDataException("invalid constructor");
                }
            }
        }

        private static Peer GetPeer(MessageConstructor message) {
            return null;
        }

        //private static Peer 

        public override void Write(BinaryWriter writer) {
            writer.Write(1);
            message.Write(writer);
        }

        private void Read(BinaryReader reader) {
            message = TL.Parse<Message>(reader);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Telegram.MTProto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    class MessageModelEncryptedDelivered : MessageModel {
        private int fromId;
        private int toId;
        private int date;
        private bool output;
        private bool unread;
        private DecryptedMessage message;
        private EncryptedFile file;

        public MessageModelEncryptedDelivered(int fromId, int toId, int date, bool output, bool unread, DecryptedMessage message, EncryptedFile file) {
            this.fromId = fromId;
            this.toId = toId;
            this.date = date;
            this.output = output;
            this.unread = unread;
            this.message = message;
            this.file = file;
        }

        public MessageModelEncryptedDelivered(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            fromId = reader.ReadInt32();
            toId = reader.ReadInt32();
            date = reader.ReadInt32();
            output = reader.ReadBoolean();
            unread = reader.ReadBoolean();
            message = TL.Parse<DecryptedMessage>(reader);
            file = TL.Parse<EncryptedFile>(reader);
        }
        public override void Write(BinaryWriter writer) {
            writer.Write(3);
            writer.Write(fromId);
            writer.Write(toId);
            writer.Write(date);
            writer.Write(output);
            writer.Write(unread);
            message.Write(writer);
            file.Write(writer);
        }

        public override BitmapImage Attachment {
            get { return null; }
        }

        public override bool IsOut {
            get { return output; }
        }

        public override int Id {
            get {
                return 0;
            }
        }

        public override bool IsService {
            get { return message.Constructor == Constructor.decryptedMessageService; }
        }

        public override bool Delivered {
            get { return true; }
        }

        public override bool IsChat {
            get { return false; }
        }

        public override string ForwardedFrom {
            get { return ""; }
        }

        public override bool IsForwarded {
            get { return false; }
        }

        public override int ForwardedId {
            get { return 0; }
        }

        public override void MarkRead() {
            unread = false;
        }

        public override string Text {
            get {
                if(message.Constructor == Constructor.decryptedMessage) {
                    return ((DecryptedMessageConstructor) message).message;
                }

                if(message.Constructor == Constructor.decryptedMessageService) {
                    DecryptedMessageServiceConstructor msg = (DecryptedMessageServiceConstructor) message;
                    DecryptedMessageAction action = msg.action;
                    switch(action.Constructor) {
                        case Constructor.decryptedMessageActionSetMessageTTL:
                            return String.Format("{0} set self-destruct timer to {1} seconds", PeerName, ((DecryptedMessageActionSetMessageTTLConstructor) action).ttl_seconds);
                        default:
                            return String.Format("{0}: service message", PeerName);
                    }
                }

                return "unknown";
            }
            set { }
        }

        private string PeerName { // "You" or fulllname
            get {
                if(output) {
                    return "You";
                } else {
                    return TelegramSession.Instance.GetUser(fromId).FullName;
                }
            }
        }

        public override string Preview {
            get { return Text; }
        }

        public override bool Unread {
            get { return unread; }
        }

        public override DateTime Timestamp {
            get {
                return DateTimeExtensions.DateTimeFromUnixTimestampSeconds(date - TelegramSession.Instance.TimeOffset);
            }
            set { }
        }

        public override MessageDeliveryState GetMessageDeliveryState() {
            return unread ? MessageDeliveryState.Delivered : MessageDeliveryState.Read;
        }

        public override string TimeString {
            get { return Formatters.FormatDialogDateTimestampUnix(date - TelegramSession.Instance.TimeOffset); }
        }

        public override UserModel Sender {
            get { return TelegramSession.Instance.GetUser(fromId); }
        }

        public override MessageDeliveryState MessageDeliveryStateProperty {
            get { return GetMessageDeliveryState(); }
        }
    }
}

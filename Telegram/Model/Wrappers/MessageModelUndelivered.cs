using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Telegram.MTProto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    class MessageModelUndelivered :MessageModel {
        public enum Type {
            Forwarded = 0,
            Photo = 1,
            Video = 2,
            Location = 3,
            Text = 4
        };

        public Type MessageType { get; set; }
        public long RandomId { get; set; }

        public MessageModelUndelivered(BinaryReader reader) {
            Read(reader);
        }

        public MessageModelUndelivered() {

        }

        private string _text;

        public override bool IsService {
            get { return false; }
        }

        public override bool Delivered {
            get { return false; }
        }

        public override string Text {
            get { return _text; }
            set { _text = value; }
        }

        public override BitmapImage Attachment {
            get { return null; }
        }

        public override bool IsOut {
            get {
                return true;
            }
        }

        public override string Preview {
            get { return Text; }
        }

        public override DateTime Timestamp { get; set; }

        public override void Write(BinaryWriter writer) {
            writer.Write(2);

            Serializers.String.write(writer, _text);
            writer.Write(DateTimeExtensions.GetUnixTimestampSeconds(Timestamp));
            writer.Write((int) MessageType);
            writer.Write(RandomId);
        }

        public void Read(BinaryReader reader) {
            _text = Serializers.String.read(reader);
            Timestamp = DateTimeExtensions.DateTimeFromUnixTimestampSeconds(reader.ReadInt64());
            MessageType = (Type) reader.ReadInt32();
            RandomId = reader.ReadInt64();
        }

        public override MessageDeliveryState GetMessageDeliveryState() {
            return MessageDeliveryState.Pending;
        }

        public override MessageDeliveryState MessageDeliveryStateProperty {
            get {
                return GetMessageDeliveryState();
            }
        }

        public override string TimeString {
            get {
                return Formatters.FormatDialogDateTimestamp(Timestamp);
            }
        }

        public override UserModel Sender {
            get {
                return TelegramSession.Instance.GetUser(TelegramSession.Instance.SelfId);
            }
        }
    }
}

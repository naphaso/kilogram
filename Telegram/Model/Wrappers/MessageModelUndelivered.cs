using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.MTProto;

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

        public MessageModelUndelivered(BinaryReader reader) {
            Read(reader);
        }

        public MessageModelUndelivered() {

        }

        private string _text;

        public override bool Delivered {
            get { return false; }
        }

        public override string Text {
            get { return _text; }
            set { _text = value; }
        }

        public override sealed DateTime Timestamp { get; set; }

        public override void Write(BinaryWriter writer) {
            writer.Write(2);

            Serializers.String.write(writer, Text);
            writer.Write(DateTimeExtensions.GetUnixTimestampSeconds(Timestamp));
            writer.Write((int) MessageType);
        }

        public void Read(BinaryReader reader) {
            _text = Serializers.String.read(reader);
            Timestamp = DateTimeExtensions.DateTimeFromUnixTimestampSeconds(reader.ReadInt64());
            MessageType = (Type) reader.ReadInt32();
        }
    }
}

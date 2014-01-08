using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogModelEncrypted : DialogModel {
        public DialogModelEncrypted(TelegramSession session) : base(session) {
            throw new NotImplementedException();
        }

        public DialogModelEncrypted(TelegramSession session, BinaryReader reader) : base(session) {
            throw new NotImplementedException();
        }

        public override BitmapImage AvatarPath {
            get { throw new NotImplementedException(); }
        }

        public override Peer Peer {
            get { throw new NotImplementedException(); }
        }

        public override DialogStatus PreviewOrAction {
            get { throw new NotImplementedException(); }
        }

        public override DialogStatus StatusOrAction {
            get { throw new NotImplementedException(); }
        }

        public override string Preview {
            get { throw new NotImplementedException(); }
        }

        public override bool IsChat {
            get { throw new NotImplementedException(); }
        }

        public override void Write(BinaryWriter writer) {
            throw new NotImplementedException();
        }

        public override void Read(BinaryReader reader) {
            throw new NotImplementedException();
        }

        public override Task SendMessage(string message) {
            throw new NotImplementedException();
        }

        public override Task RemoveAndClearDialog() {
            throw new NotImplementedException();
        }

        public override Task ClearDialogHistory() {
            throw new NotImplementedException();
        }
    }
}

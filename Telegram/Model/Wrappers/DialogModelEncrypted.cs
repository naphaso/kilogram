using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telegram.MTProto;
using Telegram.MTProto.Crypto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class DialogModelEncrypted : DialogModel {
        private EncryptedChat chat;
        private byte[] key;
        private long fingerprint;
        public DialogModelEncrypted(TelegramSession session, EncryptedChat chat) : base(session) {
            this.chat = chat;
        }

        public DialogModelEncrypted(TelegramSession session, EncryptedChat chat, byte[] key, long fingerprint) : base(session) {
            this.chat = chat;
            this.key = key;
            this.fingerprint = fingerprint;
        }

        public DialogModelEncrypted(TelegramSession session, BinaryReader reader) : base(session) {
            Read(reader);
        }


        public override Peer Peer {
            get {
                return TL.peerUser(OpponentId);
            }
        }

        public int OpponentId {
            get {
                switch (chat.Constructor) {
                    case Constructor.encryptedChat:
                        return ((EncryptedChatConstructor)chat).participant_id == session.SelfId ? ((EncryptedChatConstructor)chat).admin_id : ((EncryptedChatConstructor)chat).participant_id;
                    case Constructor.encryptedChatRequested:
                        return ((EncryptedChatRequestedConstructor)chat).participant_id == session.SelfId ? ((EncryptedChatRequestedConstructor)chat).admin_id : ((EncryptedChatRequestedConstructor)chat).participant_id;
                    case Constructor.encryptedChatWaiting:
                        return ((EncryptedChatWaitingConstructor)chat).participant_id == session.SelfId ? ((EncryptedChatWaitingConstructor)chat).admin_id : ((EncryptedChatWaitingConstructor)chat).participant_id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }


        public override string Preview {
            get { return "preview"; }
        }

        public override bool IsChat {
            get { return false; }
        }


        public override async Task SendMessage(string message) {
            long messageId = Helpers.GenerateRandomLong();
            DecryptedMessage msg = TL.decryptedMessage(messageId, Helpers.GenerateRandomBytes(128), message, TL.decryptedMessageMediaEmpty());
            byte[] data;
            using(MemoryStream memory = new MemoryStream()) {
                using(BinaryWriter writer = new BinaryWriter(memory)) {
                    msg.Write(writer);
                    data = memory.ToArray();
                }
            }

            using(MemoryStream memory = new MemoryStream()) {
                using(BinaryWriter writer = new BinaryWriter(memory)) {
                    writer.Write(data.Length);
                    data = memory.ToArray();
                }
            }

            byte[] msgKey = Helpers.CalcMsgKey(data);
            AESKeyData aesKey = Helpers.CalcKey(key, msgKey, true);
            data = AES.EncryptAES(aesKey, data);

            using(MemoryStream memory = new MemoryStream()) {
                using(BinaryWriter writer = new BinaryWriter(memory)) {
                    writer.Write(msgKey);
                    writer.Write(data);
                    data = memory.ToArray();
                }
            }

            Messages_sentEncryptedMessageConstructor sent = (Messages_sentEncryptedMessageConstructor) await session.Api.messages_sendEncrypted(TL.inputEncryptedChat(Id, AccessHash), messageId, data);
            
        }

        public override Task RemoveAndClearDialog() {
            throw new NotImplementedException();
        }

        public override Task ClearDialogHistory() {
            throw new NotImplementedException();
        }


        public void SetEncryptedChat(EncryptedChatConstructor chat) {
            this.chat = chat;

            // TODO: on property changed
        }


        public override void Write(BinaryWriter writer) {
            chat.Write(writer);
            if(key == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                Serializers.Bytes.write(writer, key);
                writer.Write(fingerprint);
            }
        }

        public override void Read(BinaryReader reader) {
            chat = TL.Parse<EncryptedChat>(reader);
            int keyExists = reader.ReadInt32();
            if(keyExists != 0) {
                key = Serializers.Bytes.read(reader);
                fingerprint = reader.ReadInt64();
            }
        }



        public int Id {
            get {
                switch (chat.Constructor) {
                    case Constructor.encryptedChat:
                        return ((EncryptedChatConstructor)chat).id;
                    case Constructor.encryptedChatRequested:
                        return ((EncryptedChatRequestedConstructor)chat).id;
                    case Constructor.encryptedChatWaiting:
                        return ((EncryptedChatWaitingConstructor)chat).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public long AccessHash {
            get {
                switch (chat.Constructor) {
                    case Constructor.encryptedChat:
                        return ((EncryptedChatConstructor)chat).access_hash;
                    case Constructor.encryptedChatRequested:
                        return ((EncryptedChatRequestedConstructor)chat).access_hash;
                    case Constructor.encryptedChatWaiting:
                        return ((EncryptedChatWaitingConstructor)chat).access_hash;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }
    }
}

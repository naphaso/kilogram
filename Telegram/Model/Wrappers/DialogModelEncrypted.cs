using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.MTProto.Crypto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class DialogModelEncrypted : DialogModel {
        private static readonly  Logger logger = LoggerFactory.getLogger(typeof(DialogModelEncrypted));

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
            logger.info("send message with key: {0}", BitConverter.ToString(key).Replace("-", "").ToLower());
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

            byte[] msgKey = CalcMsgKey(data);
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

        public void ReceiveMessage(EncryptedMessage encryptedMessage) {
            if(encryptedMessage.Constructor == Constructor.encryptedMessage) {
                EncryptedMessageConstructor encryptedMessageConstructor = (EncryptedMessageConstructor) encryptedMessage;
                byte[] data = encryptedMessageConstructor.bytes;
                byte[] msgKey;

                using(MemoryStream memory = new MemoryStream(data)) {
                    using(BinaryReader reader = new BinaryReader(memory)) {
                        msgKey = reader.ReadBytes(16);
                        data = reader.ReadBytes(data.Length - 16);
                    }
                }



                AESKeyData aesKey = Helpers.CalcKey(key, msgKey, true);
                data = AES.DecryptAES(aesKey, data);

                logger.info("plaintext data: {0}", BitConverter.ToString(data).Replace("-","").ToLower());

                byte[] calculatedMsgKey;

                using(MemoryStream memory = new MemoryStream(data)) {
                    using(BinaryReader reader = new BinaryReader(memory)) {
                        int len = reader.ReadInt32();
                        logger.info("readed len = {0}, actual len = {1}", len, data.Length - 4);
                        if(len > data.Length - 4) {
                            return;
                        }
                        calculatedMsgKey = Helpers.CalcMsgKey(data, 0, 4 + len);
                        data = reader.ReadBytes(len);
                    }
                }

                if (!msgKey.SequenceEqual(calculatedMsgKey)) {
                    logger.info("incalid msg key: data {0}, sha1 {1}, received msg key {2}", BitConverter.ToString(data), BitConverter.ToString(Helpers.sha1(data)), BitConverter.ToString(msgKey));
                    return;
                }

                DecryptedMessage decryptedMessage;

                using (MemoryStream memory = new MemoryStream(data)) {
                    using (BinaryReader reader = new BinaryReader(memory)) {
                        decryptedMessage = TL.Parse<DecryptedMessage>(reader);
                    }
                }

                

                logger.info("decrypted message: {0}", decryptedMessage);


            }
        }

        public static byte[] CalcMsgKey(byte[] data) {
            byte[] msgKey = new byte[16];
            Array.Copy(Helpers.sha1(data), 0, msgKey, 0, 16);
            return msgKey;
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

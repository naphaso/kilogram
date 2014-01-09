using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.MTProto.Components;
using Telegram.MTProto.Crypto;
using Telegram.mtproto.Crypto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class DialogModelEncrypted : DialogModel {
        private static readonly  Logger logger = LoggerFactory.getLogger(typeof(DialogModelEncrypted));

        private EncryptedChat chat;
        private byte[] key;
        private byte[] a;
        private long fingerprint;
        private int ttl = 0;
        public DialogModelEncrypted(TelegramSession session, EncryptedChat chat, byte[] a) : base(session) {
            logger.info("encrypted chat created with a: {0}", BitConverter.ToString(a).Replace("-", "").ToLower());
            this.chat = chat;
            this.a = a;
        }

        public DialogModelEncrypted(TelegramSession session, EncryptedChat chat, byte[] key, long fingerprint, byte[] a) : base(session) {
            this.chat = chat;
            this.key = key;
            this.fingerprint = EncryptedChats.CalculateKeyFingerprint(key);//;fingerprint;
            this.a = a;
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

        public override bool IsSecret {
            get { return true; }
        }


        public override async Task<bool> SendMessage(string message) {
            try {
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
                        writer.Write(data);
                        data = memory.ToArray();
                    }
                }

                byte[] msgKey = Helpers.CalcMsgKey(data);
                AESKeyData aesKey = Helpers.CalcKey(key, msgKey, true);
                data = AES.EncryptAES(aesKey, data);

                using(MemoryStream memory = new MemoryStream()) {
                    using(BinaryWriter writer = new BinaryWriter(memory)) {
                        writer.Write(fingerprint);
                        writer.Write(msgKey);
                        writer.Write(data);
                        data = memory.ToArray();
                    }
                }

                Messages_sentEncryptedMessageConstructor sent = (Messages_sentEncryptedMessageConstructor) await session.Api.messages_sendEncrypted(InputEncryptedChat, messageId, data);

                MessageModel messageModel = new MessageModelEncryptedDelivered(TelegramSession.Instance.SelfId, OpponentId, sent.date, true, true, msg, TL.encryptedFileEmpty());
                messages.Add(messageModel);

                return true;
            } catch(Exception e) {
                logger.error("send encrypted message exception: {0}", e);
                return false;
            }
        }

        public override async Task SendRead() {
            logger.info("send encrypted read history");
            var result = await TelegramSession.Instance.Api.messages_readEncryptedHistory(InputEncryptedChat, TelegramSession.Instance.Updates.Date);
            logger.info("read encrypted history result: {0}", result);
        }

        public InputEncryptedChat InputEncryptedChat {
            get {
                return TL.inputEncryptedChat(Id, AccessHash);
            }
        }

        // in background thread
        public void ReceiveMessage(EncryptedMessage encryptedMessage) {
            try {
                if(encryptedMessage.Constructor == Constructor.encryptedMessage) {
                    logger.info("simple encrypted message");
                    EncryptedMessageConstructor encryptedMessageConstructor = (EncryptedMessageConstructor) encryptedMessage;
                    byte[] data = encryptedMessageConstructor.bytes;
                    byte[] msgKey;
                    long keyFingerprint;

                    logger.info("encrypted message data: {0}", BitConverter.ToString(data).Replace("-", "").ToLower());

                    using(MemoryStream memory = new MemoryStream(data)) {
                        using(BinaryReader reader = new BinaryReader(memory)) {
                            keyFingerprint = reader.ReadInt64();
                            msgKey = reader.ReadBytes(16);
                            data = reader.ReadBytes(data.Length - 16 - 8);
                        }
                    }

                    if(fingerprint != keyFingerprint) {
                        logger.error("invalid key fingerprint");
                        return;
                    }

                    logger.info("ciphertext data: {0}", BitConverter.ToString(data).Replace("-", "").ToLower());

                    AESKeyData aesKey = Helpers.CalcKey(key, msgKey, true);
                    data = AES.DecryptAES(aesKey, data);
                    byte[] data2 = AES.EncryptAES(aesKey, data);
                    byte[] data3 = AES.DecryptAES(aesKey, data2);
                    
                    logger.info("aes equals: {0}", data.SequenceEqual(data3));
                    
                    logger.info("plaintext data: {0}", BitConverter.ToString(data).Replace("-", "").ToLower());
                    logger.info("two-transformed plaintext: {0}", BitConverter.ToString(data3).Replace("-", "").ToLower());

                    byte[] calculatedMsgKey;

                    using(MemoryStream memory = new MemoryStream(data)) {
                        using(BinaryReader reader = new BinaryReader(memory)) {
                            int len = reader.ReadInt32();
                            logger.info("readed len = {0}, actual len = {1}", len, data.Length - 4);
                            if(len < 0 || len > data.Length - 4) {
                                return;
                            }
                            calculatedMsgKey = Helpers.CalcMsgKey(data, 0, 4 + len);
                            data = reader.ReadBytes(len);
                        }
                    }

                    if(!msgKey.SequenceEqual(calculatedMsgKey)) {
                        logger.info("incalid msg key: data {0}, sha1 {1}, received msg key {2}", BitConverter.ToString(data), BitConverter.ToString(Helpers.sha1(data)), BitConverter.ToString(msgKey));
                        return;
                    }

                    DecryptedMessage decryptedMessage;

                    using(MemoryStream memory = new MemoryStream(data)) {
                        using(BinaryReader reader = new BinaryReader(memory)) {
                            //DecryptedMessageLayerConstructor layer = (DecryptedMessageLayerConstructor) TL.Parse<DecryptedMessageLayer>(reader);
//                            if(layer.layer > 8) {
//                                logger.info("encrypted message layer {0} - need upgrade", layer.layer);
//                                // TODO: notify - need upgrade
//                                return;
//                            }

                            decryptedMessage = TL.Parse<DecryptedMessage>(reader);
                        }
                    }



                    logger.info("decrypted message: {0}", decryptedMessage);

                    if(decryptedMessage.Constructor == Constructor.decryptedMessageService) {
                        DecryptedMessageAction action = ((DecryptedMessageServiceConstructor) decryptedMessage).action;
                        if(action.Constructor == Constructor.decryptedMessageActionSetMessageTTL) {
                            DecryptedMessageActionSetMessageTTLConstructor actionttl = (DecryptedMessageActionSetMessageTTLConstructor) action;
                            UpdateTTL(actionttl.ttl_seconds);
                        }
                    }

                    MessageModel messageModel = new MessageModelEncryptedDelivered(OpponentId, TelegramSession.Instance.SelfId, encryptedMessageConstructor.date, false, true, decryptedMessage, encryptedMessageConstructor.file);

                    Deployment.Current.Dispatcher.BeginInvoke(() => {
                        messages.Add(messageModel);

                        if (this == TelegramSession.Instance.Dialogs.OpenedDialog) {
                            OpenedRead();
                        }
                    });
                }
            } catch(Exception e) {
                logger.error("dialog model receive encrypted message exception: {0}", e);
            }
        }

        private void UpdateTTL(int ttlSeconds) {
            this.ttl = ttlSeconds;
        }

        public override void UpdateTypings() {
            base.UpdateTypings();

            if(ttl != 0) {
                List<MessageModel> toRemove = (from message in messages where (DateTime.Now - message.Timestamp) > TimeSpan.FromSeconds(ttl) select message).ToList();
                foreach(var messageModel in toRemove) {
                    messages.Remove(messageModel);
                }
            }
        }

        public override Task RemoveAndClearDialog() {
            throw new NotImplementedException();
        }

        public override Task ClearDialogHistory() {
            throw new NotImplementedException();
        }


        public void SetEncryptedChat(EncryptedChatConstructor chat, byte[] a) {
            this.chat = chat;

            if(a != null) {
                this.a = a;
            }

            if(this.a != null) {
                logger.info("computation key based on a: {0} and m: {1}", BitConverter.ToString(this.a).Replace("-", "").ToLower(), TelegramSession.Instance.EncryptedChats.Modulo);
                key = new BigInteger(1, chat.g_a_or_b).ModPow(new BigInteger(1, this.a), TelegramSession.Instance.EncryptedChats.Modulo).ToByteArrayUnsigned();
                fingerprint = EncryptedChats.CalculateKeyFingerprint(key);
                this.a = null;
                logger.info("new calculated key: {0}", BitConverter.ToString(key).Replace("-", "").ToLower());
            }
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

            if (a == null) {
                writer.Write(0);
            }
            else {
                writer.Write(1);
                Serializers.Bytes.write(writer, a);
            }

            if(messages == null) {
                writer.Write(0);
            } else {
                writer.Write(messages.Count);
                foreach(var messageModel in messages) {
                    messageModel.Write(writer);
                }
            }
        }

        public override void Read(BinaryReader reader) {
            chat = TL.Parse<EncryptedChat>(reader);
            int keyExists = reader.ReadInt32();
            if(keyExists != 0) {
                key = Serializers.Bytes.read(reader);
                fingerprint = reader.ReadInt64();
            }

            

            int aExists = reader.ReadInt32();
            if (aExists != 0) {
                a = Serializers.Bytes.read(reader);
            }

            int messagesCount = reader.ReadInt32();
            for(int i = 0; i < messagesCount; i++) {
                int type = reader.ReadInt32();
                switch(type) {
                    case 1:
                        messages.Add(new MessageModelDelivered(reader));
                        break;
                    case 2:
                        messages.Add(new MessageModelUndelivered(reader));
                        break;
                    case 3:
                        messages.Add(new MessageModelEncryptedDelivered(reader));
                        break;
                }
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

        public void SetA(byte[] a) {
            this.a = a;
        }
    }
}

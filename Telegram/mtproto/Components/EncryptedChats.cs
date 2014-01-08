using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telegram.Model.Wrappers;
using Telegram.mtproto.Crypto;
using Telegram.MTProto.Crypto;

namespace Telegram.MTProto.Components {


    public class EncryptedChats {
        private TelegramSession session;

        private int version = 0;
        private int g;
        private BigInteger p;

        //private XoredRandom random = new XoredRandom();
        private Random random = new Random();

        public EncryptedChats(TelegramSession session) {
            this.session = session;
        }

        private byte[] GetSaltedRandomBytes(int bytesCount, byte[] salt, int offset) {
            byte[] bytes = new byte[bytesCount];
            random.NextBytes(bytes);
            for(int i = 0; i < bytesCount; i++) {
                bytes[i] ^= salt[offset + i];
            }
            return bytes;
        }

        public async Task CreateChatRequest(InputUser user) {
            messages_DhConfig dhConfig = await session.Api.messages_getDhConfig(version, 256);
            byte[] randomSalt;
            if(dhConfig.Constructor == Constructor.messages_dhConfig) {
                Messages_dhConfigConstructor conf = (Messages_dhConfigConstructor) dhConfig;
                version = conf.version;
                g = conf.g;
                p = new BigInteger(1, conf.p);
                randomSalt = conf.random;
            }
            else if(dhConfig.Constructor == Constructor.messages_dhConfigNotModified) {
                Messages_dhConfigNotModifiedConstructor conf = (Messages_dhConfigNotModifiedConstructor) dhConfig;
                randomSalt = conf.random;
            } else {
                throw new InvalidDataException("invalid constructor");
            }

            byte[] a = GetSaltedRandomBytes(256, randomSalt, 0);
            BigInteger ga = BigInteger.ValueOf(g).ModPow(new BigInteger(1, a), p);

            int randomId = random.Next(); // also chat id
            EncryptedChat chat = await session.Api.messages_requestEncryption(user, randomId, ga.ToByteArrayUnsigned());
            UpdateChat(chat);
        }

        public void UpdateChat(EncryptedChat chat) {
            if(chat.Constructor == Constructor.encryptedChatEmpty) {
                return;
            }

            switch(chat.Constructor) {
                case Constructor.encryptedChatRequested:
                    UpdateChat((EncryptedChatRequestedConstructor)chat);
                    break;
                case Constructor.encryptedChatDiscarded:
                    UpdateChat((EncryptedChatDiscardedConstructor) chat);
                    break;
                case Constructor.encryptedChatWaiting:
                    UpdateChat((EncryptedChatWaitingConstructor)chat);
                    break;
                case Constructor.encryptedChat:
                    UpdateChat((EncryptedChatConstructor)chat);
                    break;
            }
            //

        }

        public void UpdateChat(EncryptedChatConstructor chat) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var dialogsEnum = from dialog in session.Dialogs.Model.Dialogs where dialog is DialogModelEncrypted && ((DialogModelEncrypted)dialog).Id == chat.id select (DialogModelEncrypted)dialog;
                List<DialogModelEncrypted> dialogs = dialogsEnum.ToList();
                foreach (var dialogModel in dialogs) {
                    dialogModel.SetEncryptedChat(chat);
                }
            });
        }

        public void UpdateChat(EncryptedChatDiscardedConstructor chat) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var dialogsEnum = from dialog in session.Dialogs.Model.Dialogs where dialog is DialogModelEncrypted && ((DialogModelEncrypted) dialog).Id == chat.id select dialog;
                List<DialogModel> dialogs = dialogsEnum.ToList();
                foreach(var dialogModel in dialogs) {
                    session.Dialogs.Model.Dialogs.Remove(dialogModel);
                }
            });
        }
        public void UpdateChat(EncryptedChatWaitingConstructor chat) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var echats = from dialog in session.Dialogs.Model.Dialogs where dialog is DialogModelEncrypted && ((DialogModelEncrypted)dialog).Id == chat.id select dialog;
                if (echats.Any()) {
                    // ???
                }
                else {
                    session.Dialogs.Model.Dialogs.Insert(0, new DialogModelEncrypted(session, chat));
                }
            });
        }
        public void UpdateChat(EncryptedChatRequestedConstructor chat) {
            Task.Run(() => CreateChatResponse(chat));
        }

        private async Task CreateChatResponse(EncryptedChatRequestedConstructor chat) {
            messages_DhConfig dhConfig = await session.Api.messages_getDhConfig(version, 256);
            byte[] randomSalt;
            if (dhConfig.Constructor == Constructor.messages_dhConfig) {
                Messages_dhConfigConstructor conf = (Messages_dhConfigConstructor)dhConfig;
                version = conf.version;
                g = conf.g;
                p = new BigInteger(1, conf.p);
                randomSalt = conf.random;
            }
            else if (dhConfig.Constructor == Constructor.messages_dhConfigNotModified) {
                Messages_dhConfigNotModifiedConstructor conf = (Messages_dhConfigNotModifiedConstructor)dhConfig;
                randomSalt = conf.random;
            }
            else {
                throw new InvalidDataException("invalid constructor");
            }

            byte[] b = GetSaltedRandomBytes(256, randomSalt, 0);
            BigInteger bInt = new BigInteger(1, b);
            byte[] gb = BigInteger.ValueOf(g).ModPow(bInt, p).ToByteArrayUnsigned();
            byte[] key = new BigInteger(1, chat.g_a).ModPow(bInt, p).ToByteArrayUnsigned();
            long fingerprint = CalculateKeyFingerprint(key);

            EncryptedChat acceptedChat = await session.Api.messages_acceptEncryption(TL.inputEncryptedChat(chat.id, chat.access_hash), gb, fingerprint);


            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var echats = from dialog in session.Dialogs.Model.Dialogs where dialog is DialogModelEncrypted && ((DialogModelEncrypted)dialog).Id == chat.id select dialog;
                if (echats.Any()) {
                    // ???
                }
                else {
                    session.Dialogs.Model.Dialogs.Insert(0, new DialogModelEncrypted(session, acceptedChat, key, fingerprint));
                }
            });
        }

        private long CalculateKeyFingerprint(byte[] key) {
            using (SHA1 hash = new SHA1Managed()) {
                using (MemoryStream hashStream = new MemoryStream(hash.ComputeHash(key), false)) {
                    using (BinaryReader hashReader = new BinaryReader(hashStream)) {
                        hashReader.ReadBytes(12);
                        return hashReader.ReadInt64();
                        //auxHash = hashReader.ReadUInt64();
                        //hashReader.ReadBytes(12);
                        //keyId = hashReader.ReadUInt64();
                    }
                }
            }
        }
    }
}

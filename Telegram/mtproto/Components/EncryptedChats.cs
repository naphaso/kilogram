using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.mtproto.Crypto;

namespace Telegram.MTProto.Components {


    class EncryptedChats {
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
            else if(dhConfig.Constructor == Constructor.contacts_contactsNotModified) {
                Messages_dhConfigNotModifiedConstructor conf = (Messages_dhConfigNotModifiedConstructor) dhConfig;
                randomSalt = conf.random;
            } else {
                throw new InvalidDataException("invalid constructor");
            }

            byte[] a = GetSaltedRandomBytes(256, randomSalt, 0);
            BigInteger ga = BigInteger.ValueOf(g).ModPow(new BigInteger(1, a), p);

            int randomId = random.Next(); // also chat id
            EncryptedChat chat = await session.Api.messages_requestEncryption(user, randomId, ga.ToByteArrayUnsigned());
        }
    }
}

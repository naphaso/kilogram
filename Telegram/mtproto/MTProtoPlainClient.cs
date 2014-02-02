using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Telegram.Core.Logging;
using Telegram.MTProto.Network;

namespace Telegram.MTProto {
    
    abstract class MTProtoPlainClient : TransportGateway {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MTProtoPlainClient));

        private readonly Random random = new Random();
        private long lastMessageId;

        protected abstract void OnMTProtoReceive(byte[] response);

        public async Task ConnectAsync(TelegramDC dc, int maxRetries) {
            await base.ConnectAsync(dc, maxRetries);
        }

        private long GetNewMessageId() {
            long time = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            long newMessageId = ((time/1000 + TelegramSession.Instance.TimeOffset) << 32) |
                                ((time%1000) << 22) |
                                (random.Next(524288) << 2); // 2^19
            // [ unix timestamp : 32 bit] [ milliseconds : 10 bit ] [ buffer space : 1 bit ] [ random : 19 bit ] [ msg_id type : 2 bit ] = [ msg_id : 64 bit ]

            if(lastMessageId >= newMessageId) {
                newMessageId = lastMessageId + 4;
            }

            lastMessageId = newMessageId;
            return newMessageId;
        }

        public void Send(byte[] data) {
            using (var memoryStream = new MemoryStream()) {
                using (var binaryWriter = new BinaryWriter(memoryStream)) {
                    binaryWriter.Write((long)0);
                    binaryWriter.Write(GetNewMessageId());
                    binaryWriter.Write(data.Length);
                    binaryWriter.Write(data);

                    byte[] packet = memoryStream.ToArray();

                    logger.info("sending packet: {0}", BitConverter.ToString(packet));

                    TransportSend(packet);
                }
            }
        }

        protected override void OnReceive(byte[] packet) {
            logger.debug("network on receive: {0}", BitConverter.ToString(packet));
            using(MemoryStream memoryStream = new MemoryStream(packet)) {
                using(BinaryReader binaryReader = new BinaryReader(memoryStream)) {
                    long authKeyid = binaryReader.ReadInt64();
                    logger.debug("auth key id: {0}", authKeyid);

                    long messageId = binaryReader.ReadInt64();
                    logger.debug("message id: {0}", messageId);

                    int messageLength = binaryReader.ReadInt32();
                    logger.debug("message length: {0}", messageLength);

                    byte[] response = binaryReader.ReadBytes(messageLength);

                    logger.debug("server response: {0}", BitConverter.ToString(response));
                    OnMTProtoReceive(response);
                }
            }
        }
    }
}
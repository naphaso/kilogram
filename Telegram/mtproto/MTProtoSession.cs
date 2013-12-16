using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;

namespace Telegram.MTProto {



    abstract class MTProtoRequest {
        public abstract void OnSend(BinaryWriter writer);
        public abstract void OnResponse(BinaryReader reader);
        public abstract void OnException(Exception exception);
    }

    class MTProtoRequest<T> : MTProtoRequest {
        public MTProtoRequest(byte[] requestData) {
            this.requestData = requestData;
            this.responseCompletionSource = new TaskCompletionSource<T>();
        }

        public override void OnSend(BinaryWriter writer) {
            writer.Write(requestData);
            requestData = null;
        }

        public override void OnResponse(BinaryReader reader) {
            T response = TL.Parse<T>(reader);
            responseCompletionSource.SetResult(response);
        }

        public override void OnException(Exception exception) {
            responseCompletionSource.SetException(exception);
        }

        public Task<T> Task {
            get {
                return responseCompletionSource.Task;
            }
        }

        private byte[] requestData;
        private TaskCompletionSource<T> responseCompletionSource;
    }

    class MTProtoSession {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MTProtoSession));
        private TransportGateway gateway;
        private string host;
        private int port;
        private long session;
        private AuthKey authKey;

        private readonly Random random = new Random();
        private long lastMessageId = 0;

        public MTProtoSession(string host, int port, long session, AuthKey authKey) {
            this.host = host;
            this.port = port;
            this.session = session;
            this.authKey = authKey;
            this.gateway = new TransportGateway();
            this.gateway.Input += GatewayOnInput;
        }

        private void GatewayOnInput(object sender, byte[] data) {
            logger.info("on input event: {0}", BitConverter.ToString(data));
        }

        public async Task ConnectAsync() {
            await gateway.ConnectAsync(host, port);
        }

        private long GetNewMessageId() {
            long time = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            long newMessageId = ((time / 1000 + TelegramSettings.Instance.TimeOffset) << 32) |
                                ((time % 1000) << 22) |
                                (random.Next(524288) << 2); // 2^19
            // [ unix timestamp : 32 bit] [ milliseconds : 10 bit ] [ buffer space : 1 bit ] [ random : 19 bit ] [ msg_id type : 2 bit ] = [ msg_id : 64 bit ]

            if (lastMessageId >= newMessageId) {
                newMessageId = lastMessageId + 4;
            }

            lastMessageId = newMessageId;
            return newMessageId;
        }


        private Dictionary<long, MTProtoRequest> runningRequests = new Dictionary<long, MTProtoRequest>();
        private List<MTProtoRequest> pendingRequests = new List<MTProtoRequest>(); 
        public async Task<T> Call<T>(byte[] requestData) {
            logger.info("call data: {0}", BitConverter.ToString(requestData));
            return (T)(object)null; // TODO: remporary
            MTProtoRequest<T> request = new MTProtoRequest<T>(requestData);
            pendingRequests.Add(request);

            CheckSend();

            return await request.Task;
        }

        // is sending running
        private volatile bool sending = false;

        // check sending state and start sending
        private async Task CheckSend() {
            if(!sending) {
                sending = true;
                try {
                    StartSend();
                } finally {
                    sending = false;
                }
            }
        }


        // send all pending requests in network
        private void StartSend() {
            List<MTProtoRequest> requests = new List<MTProtoRequest>(pendingRequests);
            pendingRequests.Clear();

            if(requests.Count == 0) {
                return;
            } else if(requests.Count == 1) {
                //RawSend(0,0, requests[0]);
            } else {
                byte[] container;
                using(MemoryStream memoryStream = new MemoryStream())
                using(BinaryWriter writer = new BinaryWriter(memoryStream)) {
                    writer.Write(0x73f1f8dc);
                    writer.Write(requests.Count);
                    /*
                    writer.Write(messageId);
                    writer.Write(sequence);
                    writer.Write(size);
                    writer.Write(data);
                    */
                    container = memoryStream.ToArray();
                }

                RawSend(0,0, container);
            }
        }

        private void RawSend(long messageId, int sequence, byte[] packet) {
            
        }
    }
}

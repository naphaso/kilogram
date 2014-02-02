using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Threading;
using Coding4Fun.Toolkit.Controls;
using Ionic.Zlib;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;
using Telegram.MTProto.Network;
using Telegram.Utils;

namespace Telegram.MTProto {


    public abstract class MTProtoRequest {
        public MTProtoRequest() {
            Sended = false;
        }

        public ulong MessageId { get; set; }
        public int Sequence { get; set; }

        public bool Dirty { get; set; }

        public bool Sended { get; private set; }
        public DateTime SendTime { get; private set; }
        public bool ConfirmReceived { get; private set; }
        public abstract void OnSend(BinaryWriter writer);
        public abstract void OnResponse(BinaryReader reader);
        public abstract void OnException(Exception exception);
        public abstract bool Confirmed { get; }
        public abstract bool Responded { get; }

        public virtual void OnSendSuccess() {
            SendTime = DateTime.Now;
            Sended = true;
        }

        public virtual void OnConfirm() {
            ConfirmReceived = true;
        }

        public bool NeedResend {
            get {
                return Dirty || (Confirmed && !ConfirmReceived && DateTime.Now - SendTime > TimeSpan.FromSeconds(3));        
            }
        }
    }

    public abstract class MTProtoRequestUnconfirmed : MTProtoRequest {

        public override bool Confirmed {
            get { return false; }
        }
    }

    class MTProtoRequest<T> : MTProtoRequest {
        public MTProtoRequest(byte[] requestData) {
            this.requestData = requestData;
            this.responseCompletionSource = new TaskCompletionSource<T>();
        }

        public override bool Confirmed {
            get { return true; }
        }

        public override bool Responded {
            get { return true; }
        }

        public override void OnSend(BinaryWriter writer) {
            writer.Write(requestData);
        }

        public override void OnResponse(BinaryReader reader) {
            OnConfirm();
            try {
                responseCompletionSource.TrySetResult(TL.Parse<T>(reader));
            } catch (Exception e) {
                responseCompletionSource.TrySetException(e);
            }
        }

        public override void OnException(Exception exception) {
            responseCompletionSource.SetException(exception);
        }

        public Task<T> Task {
            get {
                return responseCompletionSource.Task;
            }
        }

        public override void OnConfirm() {
            base.OnConfirm();
            requestData = null;
        }

        private byte[] requestData;
        private TaskCompletionSource<T> responseCompletionSource;
    }

    class MTProtoInitRequest : MTProtoRequest {
        private TaskCompletionSource<Config> responseCompletionSource = new TaskCompletionSource<Config>();

        public override void OnSend(BinaryWriter writer) {
            writer.Write(0x39620c41); // invokeWithLayer10#39620c41, invokeWithLayer11#a6b88fdf
            writer.Write(0x69796de9); // initConnection
            writer.Write(1097); // api id
            Serializers.String.write(writer, "WinPhone Emulator"); // device model
            Serializers.String.write(writer, "WinPhone 8.0"); // system version
            Serializers.String.write(writer, "1.0-SNAPSHOT"); // app version
            Serializers.String.write(writer, "en"); // lang code

            writer.Write(0xc4f9186b); // help.getConfig
        }

        public override void OnResponse(BinaryReader reader) {
            uint code = reader.ReadUInt32();
            ConfigConstructor config = new ConfigConstructor();
            config.Read(reader);
            responseCompletionSource.SetResult(config);
        }

        public override void OnException(Exception exception) {
            responseCompletionSource.SetException(exception);
        }

        public Task<Config> Task {
            get {
                return responseCompletionSource.Task;
            }
        }

        public override bool Responded {
            get { return true; }
        }

        public override void OnSendSuccess() {
            
        }

        public override bool Confirmed {
            get { return true; }
        }

        
    }

    class MTProtoAckRequest : MTProtoRequest {
        public List<ulong> msgs;

        public MTProtoAckRequest(List<ulong> msgs) {
            this.msgs = msgs;
        }

        public override void OnSend(BinaryWriter writer) {
            writer.Write(0x62d6b459); // msgs_ack
            writer.Write(0x1cb5c415); // Vector
            writer.Write(msgs.Count);
            foreach (ulong messageId in msgs) {
                writer.Write(messageId);
            }
        }

        public override void OnResponse(BinaryReader reader) {
            //throw new NotImplementedException();
        }

        public override void OnException(Exception exception) {
            //throw new NotImplementedException();
        }

        public override bool Confirmed {
            get { return false; }
        }

        public override bool Responded {
            get { return false; }
        }

        public override void OnSendSuccess() {
            
        }
    }

    public delegate void UpdatesHandler(Updates updates);

    public delegate void ReconnectHandler();

    public delegate void BrokenSessionHandler();
    public class MTProtoGateway : IDisposable {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MTProtoGateway));
        private volatile TransportGatewayAsync gateway;

        private TelegramDC dc;
        private ISession session;
        
        private ulong salt;
        private Config config;

        private readonly Random random = new Random();
        private ulong lastMessageId = 0;

        private List<ulong> needConfirmation = new List<ulong>();
        private DateTime lastSend = DateTime.Now;

        private DispatcherTimer timer = null;

        private bool highlevel;

        public event UpdatesHandler UpdatesEvent;
        public event ReconnectHandler ReconnectEvent;
        public event BrokenSessionHandler BrokenSessionEvent;

        private void OnUpdatesEvent(Updates updates) {
            var handler = UpdatesEvent;
            if (handler != null) handler(updates);
        }

        private void OnReconnectEvent() {
            var handler = ReconnectEvent;
            if (handler != null) handler();
        }

        private void OnBrokenSessionEvent() {
            var handler = BrokenSessionEvent;
            if (handler != null) handler();
        }

        public MTProtoGateway(TelegramDC dc, ISession session, bool highlevel, ulong salt) {
            this.dc = dc;
            this.session = session;
            this.highlevel = highlevel;
            this.salt = salt;
        }

        public Config Config {
            get { return config; }
        }

        public ulong Salt {
            get { return salt; }
        }

        private void GatewayOnInput(object sender, byte[] data) {

            ulong remoteSalt;
            ulong remoteSessionId;
            ulong remoteMessageId;
            int remoteSequence;
            byte[] message;

            using (MemoryStream inputStream = new MemoryStream(data))
            using(BinaryReader inputReader = new BinaryReader(inputStream)) {
                ulong remoteAuthKeyId = inputReader.ReadUInt64(); // TODO: check auth key id
                byte[] msgKey = inputReader.ReadBytes(16); // TODO: check msg_key correctness
                AESKeyData keyData = Helpers.CalcKey(dc.AuthKey.Data, msgKey, false);

                byte[] plaintext = AES.DecryptAES(keyData, inputReader.ReadBytes((int) (inputStream.Length - inputStream.Position)));

                using (MemoryStream plaintextStream = new MemoryStream(plaintext))
                using(BinaryReader plaintextReader = new BinaryReader(plaintextStream)) {
                    remoteSalt = plaintextReader.ReadUInt64();
                    remoteSessionId = plaintextReader.ReadUInt64();
                    remoteMessageId = plaintextReader.ReadUInt64();
                    remoteSequence = plaintextReader.ReadInt32();
                    int msgLen = plaintextReader.ReadInt32();
                    message = plaintextReader.ReadBytes(msgLen);
                }

                logger.info("gateway on input: {0}", BitConverter.ToString(message).Replace("-", "").ToLower());
            }

            using(MemoryStream messageStream = new MemoryStream(message, false))
            using(BinaryReader messageReader = new BinaryReader(messageStream)) {
                try {
                    processMessage(remoteMessageId, remoteSequence, messageReader);
                } catch(Exception e) {
                    logger.error("failed to process message: {0}", e);
                }
            }
        }


        public async Task ConnectAsync() {
            try {
                logger.info("mtptoto gateway connect async");

                if (dc.AuthKey == null) {
                    dc.AuthKey = await new Authenticator().Generate(dc, 5);
                }

                config = null;
                //await gateway.ConnectAsync(dc, -1);
                if (gateway != null) {
                    logger.debug("disposing old gateway");
                    gateway.Dispose();
                }

                // creating new gateway, start task to connect
                gateway = new TransportGatewayAsync(dc);
                gateway.InputEvent += GatewayOnInput;
                gateway.ConnectedEvent += async delegate {
                                              foreach (var runningRequest in runningRequests.Values) {
                                                  runningRequest.Dirty = true;
                                              }

                                              await Submit(null);
                                          };
                Task.Run(() => gateway.Run());

                DelayTask();

                if(highlevel) {
                    /*
                    while(true) {
                        try {
                            MTProtoInitRequest initRequest = new MTProtoInitRequest();
                            await Submit(initRequest);
                            config = await initRequest.Task;
                            break;
                        } catch(MTProtoBadMessageException e) {
                            throw new MTProtoBrokenSessionException();
                        } catch(Exception e) {
                            logger.info("init connection failed: {0}", e);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }*/

                    // it's highlevel mtproto gateway, sending init request
                    byte[] requestBytes;
                    MTProtoInitRequest request = new MTProtoInitRequest();
                    using(MemoryStream memory = new MemoryStream())
                    using (BinaryWriter writer = new BinaryWriter(memory)) {
                        request.OnSend(writer);
                        requestBytes = memory.ToArray();
                    }

                    config = await Call<Config>(requestBytes);

                    if(config == null) {
                        logger.debug("failed to get config");
                        throw new MTProtoInitException();
                    }


                    gateway.ConnectedEvent += () => ReconnectEvent();
                }

                logger.info("connection established, config: {0}", config);

            } catch(Exception ex) {
                logger.error("mtproto gateway connect async exception {0}", ex);
                throw ex;
            }
        }

        private async Task DelayTask() {
            await Task.Delay(TimeSpan.FromSeconds(2));
            timerDispatcher();

            if (gateway != null) {
                DelayTask();
            }
        }


        private ulong GetNewMessageId() {
            long time = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            ulong newMessageId = (ulong) ((time / 1000 + TelegramSession.Instance.TimeOffset) << 32) |
                                 (ulong) ((time % 1000) << 22) |
                                 (ulong) (random.Next(524288) << 2); // 2^19
            // [ unix timestamp : 32 bit] [ milliseconds : 10 bit ] [ buffer space : 1 bit ] [ random : 19 bit ] [ msg_id type : 2 bit ] = [ msg_id : 64 bit ]

            if (lastMessageId >= newMessageId) {
                newMessageId = lastMessageId + 4;
            }

            lastMessageId = newMessageId;
            return newMessageId;
        }


        private Dictionary<ulong, MTProtoRequest> runningRequests = new Dictionary<ulong, MTProtoRequest>();
        //private List<MTProtoRequest> pendingRequests = new List<MTProtoRequest>();

        public async Task<T> Call<T>(byte[] requestData) {
            logger.info("call data: {0}", BitConverter.ToString(requestData));
            MTProtoRequest<T> request = new MTProtoRequest<T>(requestData);
            //Task.Run(() => Submit(request));
            try {
                await Submit(request);
            } catch (Exception e) {
                logger.warning("failed to submit request: {0}", e);
            }

            return await request.Task;
            /*
            try {
                return await request.Task;
            } catch (MTProtoBadServerSaltException e) {
                salt = e.Salt;
            } catch (MTProtoBadMessageException e) {
                logger.warning(("bad msg notification, possible session is broken"));
                OnBrokenSessionEvent();
            }*/
        }

        public async Task Submit(MTProtoRequest requestToSubmit) {
            List<MTProtoRequest> requests = new List<MTProtoRequest>();
            if (requestToSubmit != null) {
                requests.Add(requestToSubmit);    
            }
            

            lock (runningRequests) {
                foreach (var runningRequest in runningRequests.Values) {
                    if (runningRequest.NeedResend) {
                        requests.Add(runningRequest);
                    }
                }

                if (needConfirmation.Count > 0) {
                    requests.Add(makeAck());
                }

                lastSend = DateTime.Now;
            }

            // nothing to send (impossible)
            if (requests.Count == 0) {
                return;
            }

            // send one packet
            if (requests.Count == 1 && requestToSubmit != null) {
                MTProtoRequest request = requests[0];
                request.MessageId = GetNewMessageId();
                request.Sequence = session.GenerateSequence(request.Confirmed);

                logger.info("send single request: {0} with id {1}", request, request.MessageId);

                byte[] requestBytes;
                using (MemoryStream memory = new MemoryStream()) {
                    using (BinaryWriter writer = new BinaryWriter(memory)) {
                        request.OnSend(writer);
                        requestBytes = memory.ToArray();
                    }
                }

                if (request.Responded) {
                    lock (runningRequests) {
                        if (!runningRequests.ContainsKey(request.MessageId)) {
                            runningRequests[request.MessageId] = request;
                        }
                        //runningRequests.Add(request.MessageId, request);
                    }
                }

                await RawSend(request.MessageId, request.Sequence, requestBytes);
                request.OnSendSuccess();
    
                return;
            }

            // send multiple packets in container
            byte[] container;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream)) {
                writer.Write(0x73f1f8dc);
                writer.Write(requests.Count);

                foreach (MTProtoRequest request in requests) {
                    if (runningRequests.ContainsKey(request.MessageId)) {
                        runningRequests.Remove(request.MessageId);
                    }

                    if (!request.Sended) {
                        request.MessageId = GetNewMessageId();
                        request.Sequence = session.GenerateSequence(request.Confirmed);
                    }
                    
                    logger.info("send request in container: {0} with id {1}", request, request.MessageId);

                    writer.Write(request.MessageId);
                    writer.Write(request.Sequence);

                    byte[] packet;
                    using (MemoryStream packetMemoryStream = new MemoryStream()) {
                        using (BinaryWriter packetWriter = new BinaryWriter(packetMemoryStream)) {
                            request.OnSend(packetWriter);
                            packet = packetMemoryStream.ToArray();
                        }
                    }

                    writer.Write(packet.Length);
                    writer.Write(packet);

                    if (request.Responded) {
                        lock (runningRequests) {
                            if (!runningRequests.ContainsKey(request.MessageId)) {
                                runningRequests.Add(request.MessageId, request);
                            }
                        }
                        //runningRequests.Add(request.MessageId, request);
                    }

                    request.OnSendSuccess();
                }

                container = memoryStream.ToArray();
            }

            await RawSend(GetNewMessageId(), session.GenerateSequence(false), container);
        }



        private MTProtoAckRequest makeAck() {
            if(needConfirmation.Count == 0) {
                return null;
            }

            List<ulong> confirmations = new List<ulong>(needConfirmation);
            needConfirmation.Clear();

            logger.info("make ack for messages {0}", String.Join(", ", confirmations.Select((k)=>k.ToString())));

            return new MTProtoAckRequest(confirmations);
        }

        private async Task timerDispatcher() {
            logger.info("timer tick, last send {0} seconds ago", (DateTime.Now - lastSend).TotalSeconds);
            if((DateTime.Now - lastSend).TotalSeconds > 3) {
                if (needConfirmation.Count > 0) {
                    try {
                        logger.debug("need confirmation force sending");
                        await Submit(null);
                    } catch {}
                } else {
                    foreach (var runningRequest in runningRequests.Values) {
                        logger.info("running request, need resend = {0}", runningRequest.NeedResend);
                        if (runningRequest.NeedResend) {
                            try {
                                await Submit(null);
                            } catch {}
                            break;
                        }
                    }
                }
            }
        }
       
        private MemoryStream makeMemory(int len) {
            return new MemoryStream(new byte[len], 0, len, true, true);
        }

        private async Task RawSend(ulong messageId, int sequence, byte[] packet) {
            logger.info("raw send packet...");
            byte[] msgKey;
            byte[] ciphertext;
            using (MemoryStream plaintextPacket = makeMemory(8 + 8 + 8 + 4 + 4 + packet.Length)) {
                using(BinaryWriter plaintextWriter = new BinaryWriter(plaintextPacket)) {
                    plaintextWriter.Write(salt);
                    plaintextWriter.Write(session.Id);
                    plaintextWriter.Write(messageId);
                    plaintextWriter.Write(sequence);
                    plaintextWriter.Write(packet.Length);
                    plaintextWriter.Write(packet);

                    logger.debug("raw send: {0}", BitConverter.ToString(plaintextPacket.GetBuffer()).Replace("-", "").ToLower());

                    msgKey = Helpers.CalcMsgKey(plaintextPacket.GetBuffer());
                    ciphertext = AES.EncryptAES(Helpers.CalcKey(dc.AuthKey.Data, msgKey, true), plaintextPacket.GetBuffer());
                }
            }

            using (MemoryStream ciphertextPacket = makeMemory(8 + 16 + ciphertext.Length)) {
                using (BinaryWriter writer = new BinaryWriter(ciphertextPacket)) {
                    writer.Write(dc.AuthKey.Id);
                    writer.Write(msgKey);
                    writer.Write(ciphertext);

                    logger.debug("encrypted send: {0}", BitConverter.ToString(ciphertextPacket.GetBuffer()).Replace("-","").ToLower());

                    await gateway.Send(ciphertextPacket.GetBuffer());
                }
            }
        }


        private bool processMessage(ulong messageId, int sequence, BinaryReader messageReader) {
            // TODO: check salt
            // TODO: check sessionid
            // TODO: check seqno

            //logger.debug("processMessage: msg_id {0}, sequence {1}, data {2}", BitConverter.ToString(((MemoryStream)messageReader.BaseStream).GetBuffer(), (int) messageReader.BaseStream.Position, (int) (messageReader.BaseStream.Length - messageReader.BaseStream.Position)).Replace("-","").ToLower());

            if (sequence % 2 == 1) {
                needConfirmation.Add(messageId);
            }

            uint code = messageReader.ReadUInt32();
            messageReader.BaseStream.Position -= 4;
            switch (code) {
                case 0x73f1f8dc: // container
                    logger.debug("MSG container");
                    return HandleContainer(messageId, sequence, messageReader);
                case 0x7abe77ec: // ping
                    logger.debug("MSG ping");
                    return HandlePing(messageId, sequence, messageReader);
                case 0x347773c5: // pong
                    logger.debug("MSG pong");
                    return HandlePong(messageId, sequence, messageReader);
                case 0xae500895: // future_salts
                    logger.debug("MSG future_salts");
                    return HandleFutureSalts(messageId, sequence, messageReader);
                case 0x9ec20908: // new_session_created
                    logger.debug("MSG new_session_created");
                    return HandleNewSessionCreated(messageId, sequence, messageReader);
                case 0x62d6b459: // msgs_ack
                    logger.debug("MSG msds_ack");
                    return HandleMsgsAck(messageId, sequence, messageReader);
                case 0xedab447b: // bad_server_salt
                    logger.debug("MSG bad_server_salt");
                    return HandleBadServerSalt(messageId, sequence, messageReader);
                case 0xa7eff811: // bad_msg_notification
                    logger.debug("MSG bad_msg_notification");
                    return HandleBadMsgNotification(messageId, sequence, messageReader);
                case 0x276d3ec6: // msg_detailed_info
                    logger.debug("MSG msg_detailed_info");
                    return HandleMsgDetailedInfo(messageId, sequence, messageReader);
                case 0xf35c6d01: // rpc_result
                    logger.debug("MSG rpc_result");
                    return HandleRpcResult(messageId, sequence, messageReader);
                case 0x3072cfa1: // gzip_packed
                    logger.debug("MSG gzip_packed");
                    return HandleGzipPacked(messageId, sequence, messageReader);
                case 0xe317af7e:
                case 0xd3f45784:
                case 0x2b2fbd4e:
                case 0x78d4dec1:
                case 0x725b04c3:
                case 0x74ae4240:
                    return HandleUpdate(messageId, sequence, messageReader);
                default:
                    logger.debug("unknown message: {0}", code);
                    return false;
            }
        }

        private bool HandleUpdate(ulong messageId, int sequence, BinaryReader messageReader) {
            try {
                UpdatesEvent(TL.Parse<Updates>(messageReader));
                return true;
            } catch(Exception e) {
                logger.warning("update processing exception: {0}", e);
                return false;
            }
        }



        private bool HandleGzipPacked(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            byte[] packedData = GZipStream.UncompressBuffer(Serializers.Bytes.read(messageReader));
            using (MemoryStream packedStream = new MemoryStream(packedData, false))
            using (BinaryReader compressedReader = new BinaryReader(packedStream)) {
                processMessage(messageId, sequence, compressedReader);
            }

            return true;
        }

        private bool HandleRpcResult(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            ulong requestId = messageReader.ReadUInt64();

            MTProtoRequest request;

            lock (runningRequests) {
                if (!runningRequests.ContainsKey(requestId)) {
                    logger.warning("rpc response on unknown request: {0}", requestId);
                    messageReader.BaseStream.Position -= 12;
                    return false;
                }

                request = runningRequests[requestId];
                runningRequests.Remove(requestId);
            }

            uint innerCode = messageReader.ReadUInt32();
            if(innerCode == 0x2144ca19) { // rpc_error
                int errorCode = messageReader.ReadInt32();
                string errorMessage = Serializers.String.read(messageReader);
                logger.debug("rpc result: {0}:{1}", errorCode, errorMessage);
                request.OnException(new MTProtoErrorException(errorCode, errorMessage));
            } else if(innerCode == 0x3072cfa1) {
                // gzip_packed
                byte[] packedData = Serializers.Bytes.read(messageReader);
                using(MemoryStream packedStream = new MemoryStream(packedData, false))
                using(GZipStream zipStream = new GZipStream(packedStream, CompressionMode.Decompress))
                using(BinaryReader compressedReader = new BinaryReader(zipStream)) {
                    request.OnResponse(compressedReader);
                }
            } else {
                messageReader.BaseStream.Position -= 4;
                request.OnResponse(messageReader);
            }

            return true;
        }

        private bool HandleMsgDetailedInfo(ulong messageId, int sequence, BinaryReader messageReader) {
            return false;
        }

        private bool HandleBadMsgNotification(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            ulong requestId = messageReader.ReadUInt64();
            int requestSequence = messageReader.ReadInt32();
            int errorCode = messageReader.ReadInt32();

            logger.debug("bad_msg_notification: msgid {0}, seq {1}, errorcode {2}", requestId, requestSequence,
                         errorCode);

            if(!runningRequests.ContainsKey(requestId)) {
                logger.debug("bad msg notification on unknown request");
                return true;
            }

            OnBrokenSessionEvent();
            //MTProtoRequest request = runningRequests[requestId];
            //request.OnException(new MTProtoBadMessageException(errorCode));

            return true;
        }

        private bool HandleBadServerSalt(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            ulong badMsgId = messageReader.ReadUInt64();
            int badMsgSeqNo = messageReader.ReadInt32();
            int errorCode = messageReader.ReadInt32();
            ulong newSalt = messageReader.ReadUInt64();

            logger.debug("bad_server_salt: msgid {0}, seq {1}, errorcode {2}, newsalt {3}", badMsgId, badMsgSeqNo, errorCode, newSalt);

            salt = newSalt;
            /*
            if(!runningRequests.ContainsKey(badMsgId)) {
                logger.debug("bad server salt on unknown message");
                return true;
            }
            */
            

            //MTProtoRequest request = runningRequests[badMsgId];
            //request.OnException(new MTProtoBadServerSaltException(salt));

            return true;
        }

        private bool HandleMsgsAck(ulong messageId, int sequence, BinaryReader messageReader) {
            return false;
        }

        private bool HandleNewSessionCreated(ulong messageId, int sequence, BinaryReader messageReader) {
            return false;
        }

        private bool HandleFutureSalts(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            ulong requestId = messageReader.ReadUInt64();

            messageReader.BaseStream.Position -= 12;

            if(!runningRequests.ContainsKey(requestId)) {
                logger.info("future salts on unknown request");
                return false;
            }

            MTProtoRequest request = runningRequests[requestId];
            runningRequests.Remove(requestId);
            request.OnResponse(messageReader);

            return true;
        }

        private bool HandlePong(ulong messageId, int sequence, BinaryReader messageReader) {
            return false;
        }

        private bool HandlePing(ulong messageId, int sequence, BinaryReader messageReader) {
            return false;
        }

        private bool HandleContainer(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            int size = messageReader.ReadInt32();
            for(int i = 0; i < size; i++) {
                ulong innerMessageId = messageReader.ReadUInt64();
                int innerSequence = messageReader.ReadInt32();
                int innerLength = messageReader.ReadInt32();
                long beginPosition = messageReader.BaseStream.Position;
                try {
                    if(!processMessage(innerMessageId, sequence, messageReader)) {
                        messageReader.BaseStream.Position = beginPosition + innerLength;
                    }
                } catch(Exception e) {
                    logger.error("failed to process message in contailer: {0}", e);
                    messageReader.BaseStream.Position = beginPosition + innerLength;
                }
            }

            return true;
        }

        public void Dispose() {
            //if(timer != null) {
            //    timer.Stop();
            //}
            gateway.Dispose();
            gateway = null;
        }
    }
}

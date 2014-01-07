using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Coding4Fun.Toolkit.Controls;
using Ionic.Zlib;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;

namespace Telegram.MTProto {


    public abstract class MTProtoRequest {
        public abstract void OnSend(BinaryWriter writer);
        public abstract void OnResponse(BinaryReader reader);
        public abstract void OnException(Exception exception);
        public abstract bool Confirmed { get; }
        public abstract bool Responded { get; }
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

        public override bool Responded {
            get { return true; }
        }

        private byte[] requestData;
        private TaskCompletionSource<T> responseCompletionSource;
    }

    class MTProtoInitRequest : MTProtoRequest {
        private TaskCompletionSource<Config> responseCompletionSource = new TaskCompletionSource<Config>();

        public override void OnSend(BinaryWriter writer) {
            writer.Write(0xa6b88fdf); // invokeWithLayer10#39620c41, invokeWithLayer11#a6b88fdf
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
    }

    public delegate void UpdatesHandler(Updates updates);

    public class MTProtoGateway : IDisposable {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MTProtoGateway));
        private TransportGateway gateway;

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

        public MTProtoGateway(TelegramDC dc, ISession session, bool highlevel, ulong salt = 0) {
            this.dc = dc;
            this.session = session;
            this.highlevel = highlevel;
            this.salt = salt;
            gateway = new TransportGateway();
            gateway.InputEvent += GatewayOnInput;
            gateway.ConnectedEvent += delegate { CheckSend(); };
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
                AESKeyData keyData = CalcKey(msgKey, false);

                byte[] plaintext = AES.DecryptAES(keyData, inputReader.ReadBytes((int) (inputStream.Length - inputStream.Position)));

                //logger.info("decrypted plaintext: {0}", BitConverter.ToString(plaintext).Replace("-",""));

                using (MemoryStream plaintextStream = new MemoryStream(plaintext))
                using(BinaryReader plaintextReader = new BinaryReader(plaintextStream)) {
                    remoteSalt = plaintextReader.ReadUInt64();
                    remoteSessionId = plaintextReader.ReadUInt64();
                    remoteMessageId = plaintextReader.ReadUInt64();
                    remoteSequence = plaintextReader.ReadInt32();
                    int msgLen = plaintextReader.ReadInt32();
                    message = plaintextReader.ReadBytes(msgLen);
                }

                //logger.info("salt: {0}, session {1}, msgid {2}, seqno {3}", remoteSalt, remoteSessionId, remoteMessageId, remoteSequence);
                logger.info("gateway on input: {0}", BitConverter.ToString(message).Replace("-", "").ToLower());
            }

            using(MemoryStream messageStream = new MemoryStream(message, false))
            using(BinaryReader messageReader = new BinaryReader(messageStream)) {
                processMessage(remoteMessageId, remoteSequence, messageReader);    
            }
        }


        public async Task ConnectAsync() {
            try {
                logger.info("mtptoto gateway connect async");

                config = null;
                await gateway.ConnectAsync(dc, -1);

                if (dc.AuthKey == null) {
                    dc.AuthKey = await new Authenticator().Generate(dc, 5);
                }

                if(highlevel) {

                    for(int i = 0; i < 5; i++) {
                        try {
                            MTProtoInitRequest initRequest = new MTProtoInitRequest();
                            Submit(initRequest);
                            config = await initRequest.Task;
                            break;
                        } catch(MTProtoBadMessageException e) {
                            if(e.ErrorCode == 32) {
                                // broken seq
                                throw new MTProtoBrokenSessionException();
                            } else {
                                logger.info("init connection failed: {0}", e);
                            }
                        } catch(Exception e) {
                            logger.info("init connection failed: {0}", e);
                        }
                    }

                    if(config == null) {
                        throw new MTProtoInitException();
                    }
                }

                logger.info("connection established, config: {0}", config);
                //timer = new DispatcherTimer();
                //timer.Tick += new EventHandler(timerDispatcher);
                //timer.Interval = new TimeSpan(0, 0, 2);
                //timer.Start();

//                if (!gatewayConnected.Task.IsCompleted)
//                    gatewayConnected.SetResult(true);

                DelayTask();
            }
            catch (Exception ex) {
                logger.error("gateway exception {0}", ex);
                throw ex;
            }
        }

        private async Task DelayTask() {
            await Task.Delay(TimeSpan.FromSeconds(2));
            timerDispatcher(this, new EventArgs());

            if (gateway != null) {
                DelayTask();
            }
        }


        private ulong GetNewMessageId() {
            long time = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            ulong newMessageId = (ulong) ((time / 1000 + TelegramSettings.Instance.TimeOffset) << 32) |
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
        private List<MTProtoRequest> pendingRequests = new List<MTProtoRequest>(); 
        public async Task<T> Call<T>(byte[] requestData) {
            logger.info("call data: {0}", BitConverter.ToString(requestData));
            MTProtoRequest<T> request = new MTProtoRequest<T>(requestData);
            Submit(request);
            return await request.Task;
        }

        public void Submit(MTProtoRequest request) {
            pendingRequests.Add(request);
            CheckSend();
        }

        // is sending running
        private volatile bool sending = false;

        // check sending state and start sending
        private async Task CheckSend() {
            if(gateway.Connected && !sending) {
                sending = true;
                try {
                    StartSend();
                } finally {
                    sending = false;
                }
            }
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

        private void timerDispatcher(object sender, EventArgs args) {
            if((DateTime.Now - lastSend).TotalSeconds > 5) {
                if(needConfirmation.Count > 0) {
                    StartSend();
                }    
            }
        }

        // send all pending requests in network
        private void StartSend() {
            List<MTProtoRequest> requests = new List<MTProtoRequest>(pendingRequests);
            pendingRequests.Clear();

            MTProtoAckRequest confirmation = makeAck();
            if(confirmation != null) {
                requests.Add(confirmation);
            }

            lastSend = DateTime.Now;

            if(requests.Count == 0) {
                return;
            } else if(requests.Count == 1) {
                ulong messageId = GetNewMessageId();
                logger.info("send single request: {0} with id {1}", requests[0], messageId);
                using(MemoryStream memory = new MemoryStream()) {
                    using(BinaryWriter writer = new BinaryWriter(memory)) {
                        requests[0].OnSend(writer);
                        if(requests[0].Responded) {
                            runningRequests.Add(messageId, requests[0]);    
                        }
                        RawSend(messageId, session.GenerateSequence(requests[0].Confirmed), memory.ToArray());
                    }
                }
            } else {
                byte[] container;
                using(MemoryStream memoryStream = new MemoryStream())
                using(BinaryWriter writer = new BinaryWriter(memoryStream)) {
                    writer.Write(0x73f1f8dc);
                    writer.Write(requests.Count);

                    foreach(MTProtoRequest request in requests) {
                        ulong messageId = GetNewMessageId();
                        logger.info("send request in container: {0} with id {1}", request, messageId);
                        writer.Write(messageId);
                        writer.Write(session.GenerateSequence(request.Confirmed));
                        byte[] packet;
                        using(MemoryStream packetMemoryStream = new MemoryStream()) {
                            using(BinaryWriter packetWriter = new BinaryWriter(packetMemoryStream)) {
                                request.OnSend(packetWriter);
                                packet = packetMemoryStream.ToArray();
                            }
                        }
                    
                        writer.Write(packet.Length);
                        writer.Write(packet);

                        if(request.Responded) {
                            runningRequests.Add(messageId, request);
                        }
                    }
                    
                    container = memoryStream.ToArray();
                }

                RawSend(GetNewMessageId(), session.GenerateSequence(false), container);
            }
        }

        private MemoryStream makeMemory(int len) {
            return new MemoryStream(new byte[len], 0, len,
                                    true, true);
        }
        private void RawSend(ulong messageId, int sequence, byte[] packet) {
            //logger.info("raw send packet: {0}", BitConverter.ToString(packet).Replace("-", ""));
            using (MemoryStream plaintextPacket = makeMemory(8 + 8 + 8 + 4 + 4 + packet.Length)) {
                using(BinaryWriter plaintextWriter = new BinaryWriter(plaintextPacket)) {
                    plaintextWriter.Write(salt);
                    plaintextWriter.Write(session.Id);
                    plaintextWriter.Write(messageId);
                    plaintextWriter.Write(sequence);
                    plaintextWriter.Write(packet.Length);
                    plaintextWriter.Write(packet);
                   // logger.info("messageId: {0}, sessionid: {1}, sequence: {2}, packet.length {3}", messageId, session.Id, sequence, packet.Length);
                    //logger.info("plaintext: {0}", BitConverter.ToString(plaintextPacket.GetBuffer()).Replace("-",""));

                    byte[] msgKey = CalcMsgKey(plaintextPacket.GetBuffer());
                    AESKeyData key = CalcKey(msgKey, true);
                    //logger.info("AES key: {0}, iv: {1}", BitConverter.ToString(key.Key).Replace("-",""), BitConverter.ToString(key.Iv).Replace("-",""));
                    byte[] ciphertext = AES.EncryptAES(key, plaintextPacket.GetBuffer());
                    //logger.info("ciphertext: {0}", BitConverter.ToString(ciphertext).Replace("-",""));
                    using (MemoryStream ciphertextPacket = makeMemory(8 + 16 + ciphertext.Length)) {
                        using (BinaryWriter writer = new BinaryWriter(ciphertextPacket)) {
                            writer.Write(dc.AuthKey.Id);
                            writer.Write(msgKey);
                            writer.Write(ciphertext);

                            gateway.TransportSend(ciphertextPacket.GetBuffer());
                        }
                    }
                }
            }
        }

        public void RegisterApp(int vkId, string name, string phone, int age, string city) {
            using (MemoryStream memory = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(memory)) {
                writer.Write(0x9a5f6e95);
                writer.Write(vkId);
                Serializers.String.write(writer, name);
                Serializers.String.write(writer, phone);
                writer.Write(age);
                Serializers.String.write(writer, city);
                RawSend(GetNewMessageId(), session.GenerateSequence(true), memory.ToArray());
            }
        }

        private AESKeyData CalcKey(byte[] msgKey, bool client) {
            int x = client ? 0 : 8;
            byte[] buffer = new byte[48];
            
            Array.Copy(msgKey, 0, buffer, 0, 16);            // buffer[0:16] = msgKey
            Array.Copy(dc.AuthKey.Data, x, buffer, 16, 32);     // buffer[16:48] = authKey[x:x+32]
            byte[] sha1a = sha1(buffer);                     // sha1a = sha1(buffer)

            Array.Copy(dc.AuthKey.Data, 32 + x, buffer, 0, 16);   // buffer[0:16] = authKey[x+32:x+48]
            Array.Copy(msgKey, 0, buffer, 16, 16);           // buffer[16:32] = msgKey
            Array.Copy(dc.AuthKey.Data, 48 + x, buffer, 32, 16);  // buffer[32:48] = authKey[x+48:x+64]
            byte[] sha1b = sha1(buffer);                     // sha1b = sha1(buffer)

            Array.Copy(dc.AuthKey.Data, 64 + x, buffer, 0, 32);   // buffer[0:32] = authKey[x+64:x+96]
            Array.Copy(msgKey, 0, buffer, 32, 16);           // buffer[32:48] = msgKey
            byte[] sha1c = sha1(buffer);                     // sha1c = sha1(buffer)

            Array.Copy(msgKey, 0, buffer, 0, 16);            // buffer[0:16] = msgKey
            Array.Copy(dc.AuthKey.Data, 96 + x, buffer, 16, 32);  // buffer[16:48] = authKey[x+96:x+128]
            byte[] sha1d = sha1(buffer);                     // sha1d = sha1(buffer)
             
            byte[] key = new byte[32];                       // key = sha1a[0:8] + sha1b[8:20] + sha1c[4:16]
            Array.Copy(sha1a, 0, key, 0, 8);
            Array.Copy(sha1b, 8, key, 8, 12);
            Array.Copy(sha1c, 4, key, 20, 12);

            byte[] iv = new byte[32];                        // iv = sha1a[8:20] + sha1b[0:8] + sha1c[16:20] + sha1d[0:8]
            Array.Copy(sha1a, 8, iv, 0, 12);
            Array.Copy(sha1b, 0, iv, 12, 8);
            Array.Copy(sha1c, 16, iv, 20, 4);
            Array.Copy(sha1d, 0, iv, 24, 8);
            
            return new AESKeyData(key, iv);
        }

        private byte[] CalcMsgKey(byte[] data) {
            byte[] msgKey = new byte[16];
            Array.Copy(sha1(data), 4, msgKey, 0, 16);
            return msgKey;
        }

        private byte[] sha1(byte[] data) {
            using(SHA1 sha1 = new SHA1Managed()) {
                return sha1.ComputeHash(data);
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
                default:
                    //logger.debug("other message: {}", encodeHexString(unparsedPart.array(), unparsedPart.position(), unparsedPart.remaining()));
                    return HandleUpdate(messageId, sequence, messageReader);
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
            if(!runningRequests.ContainsKey(requestId)) {
                logger.warning("rpc response on unknown request: {0}", requestId);
                messageReader.BaseStream.Position -= 12;
                return false;
            }

            MTProtoRequest request = runningRequests[requestId];
            runningRequests.Remove(requestId);

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

            MTProtoRequest request = runningRequests[requestId];
            request.OnException(new MTProtoBadMessageException(errorCode));

            return true;
        }

        private bool HandleBadServerSalt(ulong messageId, int sequence, BinaryReader messageReader) {
            uint code = messageReader.ReadUInt32();
            ulong badMsgId = messageReader.ReadUInt64();
            int badMsgSeqNo = messageReader.ReadInt32();
            int errorCode = messageReader.ReadInt32();
            ulong newSalt = messageReader.ReadUInt64();

            logger.debug("bad_server_salt: msgid {0}, seq {1}, errorcode {2}, newsalt {3}", badMsgId, badMsgSeqNo, errorCode, newSalt);

            if(!runningRequests.ContainsKey(badMsgId)) {
                logger.debug("bad server salt on unknown message");
                return true;
            }

            salt = newSalt;

            MTProtoRequest request = runningRequests[badMsgId];
            request.OnException(new MTProtoBadServerSaltException());

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
                if(!processMessage(innerMessageId, sequence, messageReader)) {
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

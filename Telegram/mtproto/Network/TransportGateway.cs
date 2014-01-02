using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;

namespace Telegram.MTProto {

    public delegate void MTProtoInputHandler(object sender, byte[] data);

    public delegate void MTProtoConnectedHandler();

    class TransportGateway : IDisposable {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof (TransportGateway));
        //private AutoResetEvent pendingEvent = new AutoResetEvent(false);
        //private CancellationToken connectCancellationToken = new CancellationToken(false);
        private int connectRetries;

        private TaskCompletionSource<object> connectTaskCompletionSource;

        private Socket socket;
        private MemoryStream inputStream;
        private TelegramDC dc;
        private int endpointIndex;
        


        public event MTProtoInputHandler InputEvent;
        public event MTProtoConnectedHandler ConnectedEvent;

        enum NetworkGatewayState {
            INIT,
            CONNECTING,
            ESTABLISHED,
            DISPOSED
        }

        private NetworkGatewayState state;

        protected virtual void OnReceive(byte[] packet) {
            InputEvent(this, packet);
        }

        public TransportGateway() {
            socket = null;
            state = NetworkGatewayState.INIT;
            inputStream = new MemoryStream(4096);
        }

        private void TryReconnect() {
            if(state == NetworkGatewayState.DISPOSED) {
                connectTaskCompletionSource.SetResult(null);
                return;
            }

            if(connectRetries == 0) {
                logger.info("connect failed");
                connectTaskCompletionSource.SetException(new TransportConnectException());
                return;
            }

            logger.info("reconnect, remaining retries: {0}", connectRetries);
            state = NetworkGatewayState.INIT;
            connectRetries--;
            endpointIndex = (endpointIndex + 1)%dc.Endpoints.Count;
            Task.Delay(3000).ContinueWith(delegate { Connect(dc); });
        }


        private bool Connect(TelegramDC dc) {
            if (state == NetworkGatewayState.INIT) {
                logger.info("connecing to {0}:{1}", dc.Endpoints[endpointIndex].Host, dc.Endpoints[endpointIndex].Port);
                this.dc = dc;
                var args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint(dc.Endpoints[endpointIndex].Host, dc.Endpoints[endpointIndex].Port);
                args.Completed += OnConnected;
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                state = NetworkGatewayState.CONNECTING;
                bool async = socket.ConnectAsync(args);
                if (!async) {
                    OnConnected(this, args);
                }

                return true;
            }

            throw new InvalidOperationException("connect in not INIT state");
        }

        public async Task ConnectAsync(TelegramDC dc, int maxRetries) {
            logger.info("transport gateway connect async...");
            connectRetries = maxRetries;
            endpointIndex = 0;
            connectTaskCompletionSource = new TaskCompletionSource<object>();
            if(Connect(dc)) {
                await connectTaskCompletionSource.Task;
            } else {
                throw new TransportConnectException();
            }
            logger.info("transport gateway connected");
        }

        private void OnConnected(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.CONNECTING) {
                if (args.SocketError == SocketError.Success) {
                    logger.info("connection successfully");
                    state = NetworkGatewayState.ESTABLISHED;
                    sendCounter = 0;
                    connectTaskCompletionSource.SetResult(null);
                    ConnectedEvent();
                    ReadAsync();
                } else {
                    logger.info("connection error: {0}", args.SocketError);
                    TryReconnect();
                }
            } else {
                throw new InvalidOperationException("onConnected in not-INIT state");
            }
        }

        private void ReadAsync(byte[] buffer = null) {
            if (state == NetworkGatewayState.ESTABLISHED) {
                var args = new SocketAsyncEventArgs();
                if (buffer == null) {
                    args.SetBuffer(new byte[4096], 0, 4096);
                } else {
                    args.SetBuffer(buffer, 0, 4096);
                }

                args.Completed += OnRead;
                try {
                    bool async = socket.ReceiveAsync(args);
                    if (!async) {
                        OnRead(this, args);
                    }
                } catch (Exception e) {
                    if(state != NetworkGatewayState.DISPOSED) {
                        logger.error("exception on read: {0}", e);
                        TryReconnect();
                    }
                }
            } else {
                throw new InvalidOperationException("state is non-ESTABLISHED for read");
            }
        }

        private void OnRead(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.ESTABLISHED) {
                if (args.SocketError == SocketError.Success && socket.Connected && args.BytesTransferred > 0) {
                    //logger.debug("input transport data: {0}", BitConverter.ToString(args.Buffer, 0, args.BytesTransferred));
                    inputStream.Write(args.Buffer, 0, args.BytesTransferred);
                    CheckInput();
                    ReadAsync(args.Buffer);
                } else if(state != NetworkGatewayState.DISPOSED) {
                    logger.info("read error {0}, reconnecting", args.SocketError);
                    TryReconnect();
                }
            }
            //else {
                //throw new InvalidOperationException("state is non-ESTABLISHED");
            //}
        }

        private int ReadInt(byte[] array, long position) {
            return (array[position + 3] << 24) | (array[position + 2] << 16) | (array[position + 1] << 8) | array[position];
        }

        private void CheckInput() {
            //logger.debug("check input started");
            if (inputStream.Length < 12) {
                //logger.debug("input too short");
                return;
            }

            inputStream.Seek(0, SeekOrigin.Begin);
            BinaryReader binaryReader = new BinaryReader(inputStream);
            byte[] buffer = inputStream.GetBuffer();

            int packetLength = ReadInt(buffer, inputStream.Position);
            //logger.debug("readed packet length: {0}, buffer size: {1}", packetLength, inputStream.Length - inputStream.Position);

            while (inputStream.Length - inputStream.Position >= packetLength) {
                //logger.info("new packet success");
                int len = binaryReader.ReadInt32();
                int seq = binaryReader.ReadInt32();
                byte[] packet = binaryReader.ReadBytes(packetLength - 12);
                byte[] checksum = binaryReader.ReadBytes(4);

                Crc32 crc32 = new Crc32();
                byte[] validChecksum = crc32.ComputeHash(inputStream.GetBuffer(), (int)inputStream.Position - 4 - packet.Length - 8, 8 + packet.Length).Reverse().ToArray();

                //logger.debug("readed new network packet: len {0}, seq {1}, packet data size {2}, checksum {3}, valid checksum: {4}", len, seq, packet.Length, BitConverter.ToString(checksum).Replace("-",""), BitConverter.ToString(validChecksum).Replace("-",""));
                //logger.debug("response data: {0}", BitConverter.ToString(packet));

                if(!checksum.SequenceEqual(validChecksum)) {
                    logger.warning("invalid checksum!");
                }

                OnReceive(packet);

                if (inputStream.Length - inputStream.Position < 12) {
                    break;
                }

                packetLength = ReadInt(buffer, inputStream.Position);
            }

            long remaining = inputStream.Length - inputStream.Position;
            if (remaining == 0) {
                inputStream.Seek(0, SeekOrigin.Begin);
                inputStream.SetLength(0);
            } else {
                for (int i = 0; i < remaining; i++) {
                    buffer[i] = buffer[inputStream.Position + i];
                }

                inputStream.SetLength(remaining);
                inputStream.Seek(0, SeekOrigin.End);
            }
        }

        private int sendCounter = 0;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TransportSend(byte[] packet) {
            //logger.debug("network send packet");

            if (state != NetworkGatewayState.ESTABLISHED) {
                logger.warning("send error, state not established");
                return false;
            }

            using (MemoryStream memoryStream = new MemoryStream()) {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream)) {
                    Crc32 crc32 = new Crc32();
                    binaryWriter.Write(packet.Length + 12);
                    binaryWriter.Write(sendCounter);
                    binaryWriter.Write(packet);
                    binaryWriter.Write(crc32.ComputeHash(memoryStream.GetBuffer(), 0, 8 + packet.Length).Reverse().ToArray());

                    byte[] transportPacket = memoryStream.ToArray();

                    //logger.info("send transport packet: {0}", BitConverter.ToString(transportPacket).Replace("-",""));

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(transportPacket, 0, transportPacket.Length);

                    try {
                        socket.SendAsync(args);
                    } catch (Exception e) {
                        logger.warning("transport packet send error: {0}", e);
                        /*
                        if(state != NetworkGatewayState.DISPOSED) {
                            state = NetworkGatewayState.INIT;
                            Connect(host, port);
                        }*/
                        TryReconnect();
                        return false;
                    }

                    sendCounter++;
                    return true;
                }
            }
        }

        public void Dispose() {
            logger.info("transport connection closed");
            state = NetworkGatewayState.DISPOSED;
            try {
                socket.Close();
            } catch(Exception e) {

            }
        }

        public bool Connected {
            get {
                return state == NetworkGatewayState.ESTABLISHED;
            }
        }
    }
}

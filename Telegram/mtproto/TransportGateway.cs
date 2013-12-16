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

namespace Telegram.MTProto {

    public delegate void MTProtoInputHandler(object sender, byte[] data);

    class TransportGateway {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof (TransportGateway));
        private AutoResetEvent pendingEvent = new AutoResetEvent(false);
        private Socket socket;
        private MemoryStream inputStream;
        private string host;
        private int port;

        public event MTProtoInputHandler Input;

        enum NetworkGatewayState {
            INIT,
            CONNECTING,
            ESTABLISHED
        }

        private NetworkGatewayState state;

        protected virtual void OnReceive(byte[] packet) {
            Input(this, packet);
        }

        public TransportGateway() {
            socket = null;
            state = NetworkGatewayState.INIT;
            inputStream = new MemoryStream(4096);
        }


        private bool Connect(string host, int port) {
            if (state == NetworkGatewayState.INIT) {
                logger.info("connecing to {0}:{1}", host, port);
                this.port = port;
                this.host = host;
                var args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint(host, port);
                args.Completed += OnConnected;
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                state = NetworkGatewayState.CONNECTING;
                bool async = socket.ConnectAsync(args);
                if (!async) {
                    OnConnected(this, args);
                }

                return true;
            }
            else {
                throw new InvalidOperationException("connect in not INIT state");
            }
        }

        public async Task ConnectAsync(string host, int port) {
            if (Connect(host, port)) {
                await Wait();
            }
        }

        private void OnConnected(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.CONNECTING) {
                if (args.SocketError == SocketError.Success) {
                    logger.info("connection successfully");
                    state = NetworkGatewayState.ESTABLISHED;
                    sendCounter = 0;
                    Continue();
                    ReadAsync();
                } else {
                    logger.info("connection error {0}, sleep and reconnecting", args.SocketError);
                    state = NetworkGatewayState.INIT;
                    Thread.Sleep(3000);
                    Connect(host, port);
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
                    logger.error("exception on read: " + e);
                    state = NetworkGatewayState.INIT;
                    Thread.Sleep(3000);
                    Connect(host, port);
                }
            } else {
                throw new InvalidOperationException("state is non-ESTABLISHED for read");
            }
        }

        private void OnRead(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.ESTABLISHED) {
                if (args.SocketError == SocketError.Success && socket.Connected && args.BytesTransferred > 0) {
                    logger.debug("input transport data: {0}", BitConverter.ToString(args.Buffer, 0, args.BytesTransferred));
                    inputStream.Write(args.Buffer, 0, args.BytesTransferred);
                    CheckInput();
                    ReadAsync(args.Buffer);
                } else {
                    logger.info("read error {0}, reconnecting", args.SocketError);
                    state = NetworkGatewayState.INIT;
                    Thread.Sleep(3000);
                    Connect(host, port);
                }
            } else {
                throw new InvalidOperationException("state is non-ESTABLISHED");
            }
        }

        private int ReadInt(byte[] array, long position) {
            return (array[position + 3] << 24) | (array[position + 2] << 16) | (array[position + 1] << 8) | array[position];
        }

        private void CheckInput() {
            logger.debug("check input started");
            if (inputStream.Length < 12) {
                logger.debug("input too short");
                return;
            }

            inputStream.Seek(0, SeekOrigin.Begin);
            BinaryReader binaryReader = new BinaryReader(inputStream);
            byte[] buffer = inputStream.GetBuffer();

            int packetLength = ReadInt(buffer, inputStream.Position);
            logger.debug("readed packet length: {0}, buffer size: {1}", packetLength, inputStream.Length - inputStream.Position);

            while (inputStream.Length - inputStream.Position >= packetLength) {
                logger.info("new packet success");
                int len = binaryReader.ReadInt32();
                int seq = binaryReader.ReadInt32();
                byte[] packet = binaryReader.ReadBytes(packetLength - 12);
                int checksum = binaryReader.ReadInt32();

                logger.debug("readed new network packet: len {0}, seq {1}, packet data size {2}, checksum {3}", len, seq, packet.Length, checksum);
                logger.debug("response data: {0}", BitConverter.ToString(packet));

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
            logger.debug("network send packet");

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

                    logger.info("send transport packet: {0}", BitConverter.ToString(transportPacket));

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(transportPacket, 0, transportPacket.Length);

                    try {
                        socket.SendAsync(args);
                    } catch (Exception e) {
                        state = NetworkGatewayState.INIT;
                        Connect(host, port);
                        return false;
                    }

                    sendCounter++;
                    return true;
                }
            }
        }

        private Task Wait() {
            return Task.Run(() => pendingEvent.WaitOne());
        }

        private void Continue() {
            pendingEvent.Set();
        }
    }
}

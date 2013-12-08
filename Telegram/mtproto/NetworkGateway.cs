using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Core.Logging;

namespace Telegram.MTProto {
    


    class NetworkGateway {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof (NetworkGateway));
        private AutoResetEvent pendingEvent;
        private Socket socket;
        private string host;
        private int port;

        enum NetworkGatewayState {
            INIT,
            CONNECTING,
            ESTABLISHED,
            ERROR
        }

        private NetworkGatewayState state;


        public NetworkGateway() {
            socket = null;
            state = NetworkGatewayState.INIT;
        }


        public void ConnectAsync(string host, int port) {
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
            } else {
                throw new InvalidOperationException("connect in not INIT state");
            }
        }

        private void OnConnected(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.CONNECTING) {
                if (args.SocketError == SocketError.Success) {
                    logger.info("connection successfully");
                    state = NetworkGatewayState.ESTABLISHED;


                } else {
                    logger.info("connection error {0}, sleep and reconnecting", args.SocketError);
                    state = NetworkGatewayState.INIT;
                    Thread.Sleep(3000);
                    ConnectAsync(host, port);
                }
            } else {
                throw new InvalidOperationException("onConnected in not-INIT state");
            }
        }

        private void OnRead(object sender, SocketAsyncEventArgs args) {
            if (state == NetworkGatewayState.ESTABLISHED) {
                if (args.SocketError == SocketError.Success) {
                    string data = Encoding.UTF8.GetString(args.Buffer, 0, args.BytesTransferred);
                    logger.debug("received data: {0}", data);
                    ReadAsync();
                } else {
                    logger.info("read error {0}, reconnecting", args.SocketError);
                    state = NetworkGatewayState.INIT;
                    Thread.Sleep(3000);
                    ConnectAsync(host, port);
                }
            } else {
                throw new InvalidOperationException("state is non-ESTABLISHED");
            }
        }

        private void ReadAsync() {
            if (state == NetworkGatewayState.ESTABLISHED) {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[1024], 0, 1024);
                args.Completed += OnRead;
                bool async = socket.ReceiveAsync(args);
                if (!async) {
                    OnRead(this, args);
                }
            } else {
                throw new InvalidOperationException("state is non-ESTABLISHED for read");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Telegram.Core.Logging;

namespace Telegram.MTProto.Network {
    public class TelegramSocketException : Exception {
        public TelegramSocketException(string message, Exception innerException)
            : base(message, innerException) {
        }

        public TelegramSocketException(string message)
            : base(message) {
        }
    }

    public class AsyncSocket {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof (AsyncSocket));

        private Socket socket;

        public AsyncSocket() {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task Connect(string host, int port) {
            TaskCompletionSource<object> connectSource = new TaskCompletionSource<object>();
            try {
                var args = new SocketAsyncEventArgs {RemoteEndPoint = new DnsEndPoint(host, port)};
                EventHandler<SocketAsyncEventArgs> connectHandler = delegate(object sender, SocketAsyncEventArgs eventArgs) {
                    try {
                        if (eventArgs.LastOperation == SocketAsyncOperation.Connect && eventArgs.SocketError == SocketError.Success) {
                            connectSource.TrySetResult(null);
                        } else {
                            connectSource.TrySetException(new TelegramSocketException("unable to connect to server: " + eventArgs.LastOperation + ", " + eventArgs.SocketError));
                        }
                    } catch (Exception e) {
                        connectSource.TrySetException(new TelegramSocketException("socket exception", e));
                    }
                };

                args.Completed += connectHandler;

                if (!socket.ConnectAsync(args)) {
                    connectHandler(this, args);
                }
            } catch (Exception e) {
                connectSource.TrySetException(new TelegramSocketException("socket exception", e));
            }

            await connectSource.Task;
        }

        public async Task<byte[]> Read() {
            var readSource = new TaskCompletionSource<byte[]>();
            try {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[1024 * 8], 0, 1024 * 8);
                EventHandler<SocketAsyncEventArgs> receiveHandler = delegate(object sender, SocketAsyncEventArgs eventArgs) {
                    try {

                        if (args.LastOperation == SocketAsyncOperation.Receive && args.SocketError == SocketError.Success) {
                            if (args.BytesTransferred == 0) {
                                readSource.TrySetException(new TelegramSocketException("disconnected"));
                            } else {
                                var chunk = new byte[args.BytesTransferred];
                                Array.Copy(eventArgs.Buffer, eventArgs.Offset, chunk, 0, eventArgs.BytesTransferred);
                                readSource.TrySetResult(chunk);
                            }
                        } else {
                            readSource.TrySetException(new TelegramSocketException("read error: " + args.LastOperation + ", " + args.SocketError));
                        }
                    } catch (Exception e) {
                        readSource.TrySetException(new TelegramSocketException("read error", e));
                    }
                };

                args.Completed += receiveHandler;

                if (!socket.ReceiveAsync(args)) {
                    receiveHandler(this, args);
                }
            } catch (Exception e) {
                readSource.TrySetException(new TelegramSocketException("read error", e));
            }

            return await readSource.Task;
        }

        public async Task Send(byte[] data) {
            await Send(data, 0, data.Length);
        }

        public async Task Send(byte[] data, int offset, int length) {
            var sendSource = new TaskCompletionSource<object>();
            try {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(data, offset, length);
                EventHandler<SocketAsyncEventArgs> sendHandler = delegate(object sender, SocketAsyncEventArgs eventArgs) {
                    try {
                        if (eventArgs.SocketError == SocketError.Success) {
                            sendSource.TrySetResult(null);
                        } else {
                            sendSource.TrySetException(new TelegramSocketException("send error: " + eventArgs.SocketError));
                        }
                    } catch (Exception e) {
                        sendSource.TrySetException(new TelegramSocketException("send error", e));
                    }
                };

                args.Completed += sendHandler;

                if (!socket.SendAsync(args)) {
                    
                    sendHandler(this, args);
                }
            } catch (Exception e) {
                logger.error("sending exception: {0}", e);
                sendSource.TrySetException(new TelegramSocketException("send error", e));
            }

            await sendSource.Task;
        }

        public void Dispose() {
            try {
                socket.Close();
            } catch {}
        }
    }
}

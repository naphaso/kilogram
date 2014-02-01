using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        private Socket socket;

        public AsyncSocket() {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task Connect(string host, int port) {
            TaskCompletionSource<object> connectSource = new TaskCompletionSource<object>();
            try {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint(host, port);
                EventHandler<SocketAsyncEventArgs> connectHandler = delegate(object sender, SocketAsyncEventArgs eventArgs) {
                    try {
                        if (eventArgs.SocketError == SocketError.Success) {
                            connectSource.TrySetResult(null);
                        } else {
                            connectSource.TrySetException(
                                new TelegramSocketException("unable to connect to server: " + eventArgs.SocketError));
                        }
                    } catch (Exception e) {
                        connectSource.TrySetException(new TelegramSocketException("socket exception", e));
                    }
                };

                args.Completed += connectHandler;

                bool async = socket.ConnectAsync(args);
                if (!async) {
                    connectHandler(this, args);
                }
            } catch (Exception e) {
                connectSource.TrySetException(new TelegramSocketException("socket exception", e));
            }

            await connectSource.Task;
        }

        public async Task<byte[]> Read() {
            TaskCompletionSource<byte[]> readSource = new TaskCompletionSource<byte[]>();
            try {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[1024 * 8], 0, 1024 * 8);
                EventHandler<SocketAsyncEventArgs> receiveHandler = delegate(object sender, SocketAsyncEventArgs eventArgs) {
                    try {

                        if (args.LastOperation == SocketAsyncOperation.Receive && args.SocketError == SocketError.Success) {
                            if (args.BytesTransferred == 0) {
                                readSource.TrySetException(new TelegramSocketException("disconnected"));
                            } else {
                                byte[] chunk = new byte[args.BytesTransferred];
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

                bool async = socket.ReceiveAsync(args);
                if (!async) {
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
            TaskCompletionSource<object> sendSource = new TaskCompletionSource<object>();
            try {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
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

                bool async = socket.SendAsync(args);
                if (!async) {
                    sendHandler(this, args);
                }
            } catch (Exception e) {
                sendSource.TrySetException(new TelegramSocketException("send error", e));
            }

            await sendSource.Task;
        }


    }
}

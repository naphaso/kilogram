using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Windows.Networking.Sockets;
using Telegram.Core.Logging;
using SocketError = System.Net.Sockets.SocketError;

namespace Telegram.mtproto {
    class NetworkTest /*: IDisposable*/ {
        private static Logger logger = LoggerFactory.getLogger(typeof (NetworkTest));

        public static void start() {
            logger.debug("start network test");
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            args.RemoteEndPoint = new DnsEndPoint("naphaso.com", 8787);
            args.UserToken = socket;
            args.Completed += new EventHandler<SocketAsyncEventArgs>(onCompleted);

            

            socket.ConnectAsync(args);
        }

        static void onCompleted(object sender, SocketAsyncEventArgs args) {
            switch (args.LastOperation) {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(args);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(args);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(args);
                    break;
                default:
                    logger.debug("warning: unknown operation completed");
                    break;
            }
        }

        private static void ProcessConnect(SocketAsyncEventArgs args) {
            if (args.SocketError == SocketError.Success) {
                logger.debug("on connection completed successfully");

                byte[] buffer = Encoding.UTF8.GetBytes("Hello World");
                args.SetBuffer(buffer, 0, buffer.Length);
                Socket sock = args.UserToken as Socket;
                bool willRaiseEvent = sock.SendAsync(args);

                if (!willRaiseEvent) {
                    ProcessSend(args);
                }
            } else {
                logger.debug("on connection error: {0}", args.SocketError);
            }
        }

        private static void ProcessReceive(SocketAsyncEventArgs args) {
            if (args.SocketError == SocketError.Success) {
                logger.debug("on receive completed");
                // Received data from server
                //args.Buffer
                string data = Encoding.UTF8.GetString(args.Buffer, 0, args.BytesTransferred);
                logger.debug("received data: {0}", data);
                // Data has now been sent and received from the server. 
                // Disconnect from the server
                //Socket sock = args.UserToken as Socket;
                //sock.Shutdown(SocketShutdown.Send);
                //sock.Close();
                args.SetBuffer(new byte[4096], 0, 4096);
                Socket socket = args.UserToken as Socket;

                bool willRaiseEvent = socket.ReceiveAsync(args);
                if (!willRaiseEvent) {
                    ProcessReceive(args);
                }
                //clientDone.Set();
            } else {
                logger.debug("on receive error: {}", args.SocketError);
            }
        }

        private static void ProcessSend(SocketAsyncEventArgs args) {
            if (args.SocketError == SocketError.Success) {
                // Sent "Hello World" to the server successfully

                logger.debug("on send completed");

                //Read data sent from the server
                Socket sock = args.UserToken as Socket;

                args.SetBuffer(new byte[4096], 0, 4096);

                bool willRaiseEvent = sock.ReceiveAsync(args);

                if (!willRaiseEvent) {
                    ProcessReceive(args);
                }
            } else {
                logger.debug("on send error: {0}", args.SocketError);
                
            }
            
        }
        /*
        public void Dispose() {
            throw new NotImplementedException();
        }
         * */
    }
}

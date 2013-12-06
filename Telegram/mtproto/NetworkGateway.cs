using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.MTProto {
    
    class NetworkGateway {
        private AutoResetEvent pendingEvent;

        public async Task ConnectAsync(string host, int port) {
            var args = new SocketAsyncEventArgs {RemoteEndPoint = new DnsEndPoint(host, port)};
            args.Completed += onConnected;

            await Task.Run(() => pendingEvent.WaitOne());
            pendingEvent.WaitOne();

        }

        private void onConnected(object sender, SocketAsyncEventArgs args) {
            
        }

        private Task Wait() {
            return Task.Run(() => pendingEvent.WaitOne());
        }

        private void Continue() {
            pendingEvent.Set();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Core {
    public delegate void RequestTaskDelegate();
    public class RequestTask {
        private volatile bool running = false;
        private RequestTaskDelegate taskDelegate;

        public RequestTask(RequestTaskDelegate taskDelegate) {
            this.taskDelegate = taskDelegate;
        }

        public async Task Request() {
            if (running) {
                return;
            }

            running = true;
            try {
                await Task.Delay(50);
                await Task.Run(() => taskDelegate());
            } finally {
                running = false;    
            }
        }
    }
}

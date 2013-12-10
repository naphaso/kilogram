using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Model {
    public class DialogMessageItem {
        public string Sender { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
        public bool IsOut { get; set; }
    }
}

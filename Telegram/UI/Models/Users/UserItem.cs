using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.UI.Models.Users {
    class UserItem {
        public string Name { get; set; }
        public string LastSeen { get; set; }

        public bool Online { get; set; }

        public bool AddressBookContact { get; set; }
    }
}

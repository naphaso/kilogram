using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Model.TLWrappers;

namespace Telegram.Model {
    interface IMessageProvider {
        MessageModel GetMessage(int id);
    }

    interface IUserProvider {
        UserModel GetUser(int id);
    }

    interface IChatProvider {
        ChatModel GetChat(int id);
    }
}

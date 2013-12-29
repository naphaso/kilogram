using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Model.Wrappers;

namespace Telegram.Model {
    public interface IMessageProvider {
        MessageModel GetMessage(int id);
    }

    public interface IUserProvider {
        UserModel GetUser(int id);
    }

    public interface IChatProvider {
        ChatModel GetChat(int id);
    }
}

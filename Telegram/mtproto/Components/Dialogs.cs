using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Annotations;

namespace Telegram.MTProto.Components {

    public class DialogsState {
        public List<DialogConstructor> dialogs = new List<DialogConstructor>();
        public Dictionary<int, Message> messages = new Dictionary<int, Message>();
        public Dictionary<int, User> users = new Dictionary<int, User>();
        public Dictionary<int, Chat> chats = new Dictionary<int, Chat>();

        public int ProcessDialogs(messages_Dialogs dialogsObject) {
            List<Dialog> dialogsList;
            List<Message> messagesList;
            List<Chat> chatsList;
            List<User> usersList;
            switch(dialogsObject.Constructor) {
                case Constructor.messages_dialogs:
                    dialogsList = ((Messages_dialogsConstructor) dialogsObject).dialogs;
                    messagesList = ((Messages_dialogsConstructor) dialogsObject).messages;
                    chatsList = ((Messages_dialogsConstructor) dialogsObject).chats;
                    usersList = ((Messages_dialogsConstructor) dialogsObject).users;
                    break;
                case Constructor.messages_dialogsSlice:
                    dialogsList = ((Messages_dialogsSliceConstructor)dialogsObject).dialogs;
                    messagesList = ((Messages_dialogsSliceConstructor)dialogsObject).messages;
                    chatsList = ((Messages_dialogsSliceConstructor)dialogsObject).chats;
                    usersList = ((Messages_dialogsSliceConstructor)dialogsObject).users;
                    break;
                default:
                    return 0;
            }

            foreach (Dialog dialog in dialogsList) {
                dialogs.Add((DialogConstructor)dialog);
            }

            foreach (var message in messagesList) {
                switch (message.Constructor) {
                    case Constructor.message:
                        messages.Add(((MessageConstructor)message).id, message);
                        break;
                    case Constructor.messageForwarded:
                        messages.Add(((MessageForwardedConstructor)message).id, message);
                        break;
                    case Constructor.messageService:
                        messages.Add(((MessageServiceConstructor)message).id, message);
                        break;
                }
            }

            foreach (var user in usersList) {
                switch (user.Constructor) {
                    case Constructor.userEmpty:
                        users.Add(((UserEmptyConstructor)user).id, user);
                        break;
                    case Constructor.userSelf:
                        users.Add(((UserSelfConstructor)user).id, user);
                        break;
                    case Constructor.userContact:
                        users.Add(((UserContactConstructor)user).id, user);
                        break;
                    case Constructor.userRequest:
                        users.Add(((UserRequestConstructor)user).id, user);
                        break;
                    case Constructor.userForeign:
                        users.Add(((UserForeignConstructor)user).id, user);
                        break;
                    case Constructor.userDeleted:
                        users.Add(((UserDeletedConstructor)user).id, user);
                        break;
                }
            }

            foreach (var chat in chatsList) {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        chats.Add(((ChatEmptyConstructor)chat).id, chat);
                        break;
                    case Constructor.chat:
                        chats.Add(((ChatConstructor)chat).id, chat);
                        break;
                    case Constructor.chatForbidden:
                        chats.Add(((ChatForbiddenConstructor)chat).id, chat);
                        break;
                }
            }

            return dialogsList.Count;
        }
    }
    class Dialogs {
        private TelegramSession session;
 
        public Dialogs(TelegramSession session) {
            this.session = session;
        }

        private async Task<DialogsState> DialogsRequest() {
            DialogsState newState = new DialogsState();

            int offset = 0;
            while(true) {
                messages_Dialogs dialogsPart = await session.Api.messages_getDialogs(offset, 0, 100);
                offset += newState.ProcessDialogs(dialogsPart);

                if(dialogsPart.Constructor == Constructor.messages_dialogs) {
                    break;
                }
            }

            return newState;
        }
    }
}

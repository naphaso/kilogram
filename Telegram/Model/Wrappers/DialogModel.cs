using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Phone.Reactive;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogModel {
        private DialogConstructor dialog;
        private IMessageProvider messageProvider;
        private IUserProvider userProvider;
        private IChatProvider chatProvider;

        private ObservableCollection<MessageModel> messages;
        private Dictionary<int, UserModel> users;
        private Dictionary<int, ChatModel> chats; 

        private TelegramSession session;

        public DialogModel(Dialog dialog, TelegramSession session, IMessageProvider messageProvider, IUserProvider userProvider, IChatProvider chatProvider) {
            this.dialog = (DialogConstructor) dialog;
            this.messageProvider = messageProvider;
            this.userProvider = userProvider;
            this.chatProvider = chatProvider;
            this.session = session;
        }

        public Dialog RawDialog {
            get {
                return dialog;
            }
        }

        public string Title {
            get {
                string title = "";
                switch (dialog.peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = dialog.peer as PeerChatConstructor;
                        // check null
                        var chatModel = chatProvider.GetChat(peerChat.chat_id);
                        title = chatModel.Title;                        
                        break;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        var userModel = userProvider.GetUser(peerUser.user_id);
                        title = userModel.FullName;
                        break;
                }

                return title;
            }
        }

        public ObservableCollection<Message> Messages {
            get {
                if(messages == null) {
                    messages = new ObservableCollection<Message>();
                    MessagesRequest();
                } else {
                    return messages;
                }
            }
        } 


        private async Task MessagesRequest() {
            messages_Messages loadedMessages = await session.Api.messages_getHistory(TLStuff.PeerToInputPeer(dialog.peer), 0, -1, 100);
            List<Message> messagesList;
            List<Chat> chatsList;
            List<User> usersList;

            switch(loadedMessages.Constructor) {
                case Constructor.messages_messages:
                    chatsList = ((Messages_messagesConstructor) loadedMessages).chats;
                    messagesList = ((Messages_messagesConstructor)loadedMessages).messages;
                    usersList = ((Messages_messagesConstructor)loadedMessages).users;
                    break;
                case Constructor.messages_messagesSlice:
                    chatsList = ((Messages_messagesSliceConstructor) loadedMessages).chats;
                    messagesList = ((Messages_messagesSliceConstructor)loadedMessages).messages;
                    usersList = ((Messages_messagesSliceConstructor)loadedMessages).users;
                    break;
                default:
                    return;
            }


            foreach(var user in usersList) {
                var userModel = new UserModel(user);
                users.Add(userModel.Id, userModel);
            }

            foreach(var chat in chatsList) {
                var chatModel = new ChatModel(chat);
                chats.Add(chatModel.Id, chatModel);
            }

            foreach (var message in messagesList) {
                messages.Add(new MessageModel(message));
            }
        }
    }
}

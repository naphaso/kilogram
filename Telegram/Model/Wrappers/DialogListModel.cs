using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Telegram.Core.Logging;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogListModel : IMessageProvider, IUserProvider, IChatProvider {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListModel));
        private ObservableCollection<DialogModel> dialogs = new ObservableCollection<DialogModel>();
        private Dictionary<int, MessageModel> messages = new Dictionary<int, MessageModel>();
        private Dictionary<int, UserModel> users = new Dictionary<int, UserModel>();
        private Dictionary<int, ChatModel> chats = new Dictionary<int, ChatModel>();

        private TelegramSession session;

        public DialogListModel(TelegramSession session) {
            this.session = session;
        }

        public ObservableCollection<DialogModel> Dialogs {
            get { return dialogs; }
        }

        public Dictionary<int, MessageModel> Messages {
            get { return messages; }
        }

        public Dictionary<int, UserModel> Users {
            get { return users; }
        }

        public Dictionary<int, ChatModel> Chats {
            get { return chats; }
        }

        public int ProcessDialogs(messages_Dialogs dialogsObject) {
            List<Dialog> dialogsList;
            List<Message> messagesList;
            List<Chat> chatsList;
            List<User> usersList;
            switch (dialogsObject.Constructor) {
                case Constructor.messages_dialogs:
                    dialogsList = ((Messages_dialogsConstructor)dialogsObject).dialogs;
                    messagesList = ((Messages_dialogsConstructor)dialogsObject).messages;
                    chatsList = ((Messages_dialogsConstructor)dialogsObject).chats;
                    usersList = ((Messages_dialogsConstructor)dialogsObject).users;
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

            logger.info("process dialogs: {0} dialogs, {1} messages, {2} chats, {3} users", dialogsList.Count, messagesList.Count, chatsList.Count, usersList.Count);

            foreach (Dialog dialog in dialogsList) {
                dialogs.Add(new DialogModel(dialog, session));
            }

            foreach (var message in messagesList) {
                var messageModel = new MessageModel(message);
                messages.Add(messageModel.Id, messageModel);
            }

            foreach (var user in usersList) {
                var userModel = new UserModel(user);
                users.Add(userModel.Id, userModel);
            }

            foreach (var chat in chatsList) {
                var chatModel = new ChatModel(chat);
                chats.Add(chatModel.Id, chatModel);
            }

            return dialogsList.Count;
        }

        public void save(BinaryWriter writer) {
            // dialogs
            writer.Write(dialogs.Count);
            foreach (var dialog in dialogs) {
                dialog.RawDialog.Write(writer);
            }

            // messages
            writer.Write(messages.Count);
            foreach (var message in messages) {
                writer.Write(message.Key);
                message.Value.RawMessage.Write(writer);
            }

            // users
            writer.Write(users.Count);
            foreach (var user in users) {
                writer.Write(user.Key);
                user.Value.RawUser.Write(writer);
            }

            // chats
            writer.Write(chats.Count);
            foreach (var chat in chats) {
                writer.Write(chat.Key);
                chat.Value.RawChat.Write(writer);
            }
        }

        public void load(BinaryReader reader) {
            // dialogs
            int dialogsCount = reader.ReadInt32();
            for (int i = 0; i < dialogsCount; i++) {
                dialogs.Add(new DialogModel(TL.Parse<Dialog>(reader), session));
            }

            // messages
            int messagesCount = reader.ReadInt32();
            for (int i = 0; i < messagesCount; i++) {
                messages.Add(reader.ReadInt32(), new MessageModel(TL.Parse<Message>(reader)));
            }

            // users
            int usersCount = reader.ReadInt32();
            for (int i = 0; i < usersCount; i++) {
                users.Add(reader.ReadInt32(), new UserModel(TL.Parse<User>(reader)));
            }

            // chats
            int chatsCount = reader.ReadInt32();
            for (int i = 0; i < chatsCount; i++) {
                chats.Add(reader.ReadInt32(), new ChatModel(TL.Parse<Chat>(reader)));
            }
        }

        public void Replace(DialogListModel state) {
            dialogs.Clear();
            chats.Clear();
            messages.Clear();
            users.Clear();

            foreach (var user in state.users) {
                users.Add(user.Key, user.Value);
            }

            foreach (var message in state.messages) {
                messages.Add(message.Key, message.Value);
            }

            foreach (var chat in state.chats) {
                chats.Add(chat.Key, chat.Value);
            }

            foreach (var dialogConstructor in state.dialogs) {
                dialogs.Add(dialogConstructor);
            }
        }

        public MessageModel GetMessage(int id) {
            return messages[id];
        }

        public UserModel GetUser(int id) {
            return users[id];
        }

        public ChatModel GetChat(int id) {
            return chats[id];
        }
    }
}

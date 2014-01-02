using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Telegram.Core.Logging;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogListModel {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListModel));
        private ObservableCollection<DialogModel> dialogs = new ObservableCollection<DialogModel>();

        private TelegramSession session;

        public DialogListModel(TelegramSession session) {
            this.session = session;
        }

        public ObservableCollection<DialogModel> Dialogs {
            get { return dialogs; }
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

            foreach (var user in usersList) {
                session.SaveUser(user);
            }

            foreach (var chat in chatsList) {
                session.SaveChat(chat);
            }

            Dictionary<int, MessageModel> messagesMap = new Dictionary<int, MessageModel>();

            foreach (var message in messagesList) {
                var messageModel = new MessageModel(message);
                messagesMap.Add(messageModel.Id, messageModel);
            }

            foreach (Dialog dialog in dialogsList) {
                dialogs.Add(new DialogModel(dialog, session, messagesMap));
            }

            return dialogsList.Count;
        }

        public void ProcessNewMessage(Message message) {
            MessageModel messageModel = new MessageModel(message);
        }

        public void save(BinaryWriter writer) {
            // dialogs
            writer.Write(dialogs.Count);
            foreach (var dialog in dialogs) {
                dialog.Write(writer);
            }
        }

        public void load(BinaryReader reader) {
            logger.info("loading dialog list model");
            // dialogs
            int dialogsCount = reader.ReadInt32();
            logger.info("dialogs count {0}", dialogsCount);
            for (int i = 0; i < dialogsCount; i++) {
                dialogs.Add(new DialogModel(session, reader));
            }
        }




    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class DialogListModel {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogListModel));
        private ObservableCollectionUI<DialogModel> dialogs = new ObservableCollectionUI<DialogModel>();

        private TelegramSession session;

        public DialogListModel(TelegramSession session) {
            this.session = session;
        }

        public DialogListModel(TelegramSession session, BinaryReader reader) : this(session) {
            Read(reader);
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
                // FIXME: review
                var messageModel = new MessageModelDelivered(message);
                messagesMap.Add(messageModel.Id, messageModel);
            }

            foreach (Dialog dialog in dialogsList) {
                dialogs.Add(new DialogModel(dialog, session, messagesMap));
            }

            return dialogsList.Count;
        }

        public void ProcessNewMessage(Message message) {

            logger.info("process new message: {0}", message);
            MessageModelDelivered messageModel = new MessageModelDelivered(message);
            Peer targetPeer = messageModel.Peer;

            DialogModel targetDialogModel = null;

            foreach(DialogModel dialogModel in dialogs) {
                if (TLStuff.PeerEquals(dialogModel.Peer, targetPeer)) {
                    targetDialogModel = dialogModel;
                    break;
                }
            }

            if(targetDialogModel == null) {
                logger.info("target dialog not found, creating new...");
                targetDialogModel = new DialogModel(messageModel, session);
                    dialogs.Insert(0, targetDialogModel);
            } else {
                logger.info("target dialog found, rearrange...");
                    dialogs.Remove(targetDialogModel);
                    dialogs.Insert(0, targetDialogModel);
            }

            targetDialogModel.ProcessNewMessage(messageModel);
        }

        public void Write(BinaryWriter writer) {
            // dialogs
            writer.Write(dialogs.Count);
            foreach (var dialog in dialogs) {
                dialog.Write(writer);
            }
        }

        private void Read(BinaryReader reader) {
            logger.info("loading dialog list model");
            // dialogs
            int dialogsCount = reader.ReadInt32();
            logger.info("dialogs count {0}", dialogsCount);
            for (int i = 0; i < dialogsCount; i++) {
                dialogs.Add(new DialogModel(session, reader));
            }
        }


        public void SetUserTyping(int userid) {
            foreach (var dialogModel in dialogs) {
                if (dialogModel.Peer.Constructor == Constructor.peerUser) {
                    if (((PeerUserConstructor)dialogModel.Peer).user_id == userid) {
                        dialogModel.SetTyping();
                        break;
                    }
                }
            }
        }

        public void SetUserTyping(int chatid, int userid) {
            foreach (var dialogModel in dialogs) {
                if (dialogModel.Peer.Constructor == Constructor.peerChat) {
                    if (((PeerChatConstructor)dialogModel.Peer).chat_id == chatid) {
                        dialogModel.SetTyping(userid);
                        break;
                    }
                }
            }
        }
    }
}

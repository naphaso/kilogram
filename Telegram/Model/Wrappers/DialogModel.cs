
﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Phone.Reactive;
﻿using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class DialogModel {
        private DialogConstructor dialog;
        private ObservableCollection<MessageModel> messages;
        private TelegramSession session;

        public DialogModel(Dialog dialog, TelegramSession session) {
            this.dialog = (DialogConstructor) dialog;
            this.session = session;
        }

        private Dialog RawDialog {
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
                        var chatModel = session.GetChat(peerChat.chat_id);
                        title = chatModel.Title;                        
                        break;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        var userModel = session.GetUser(peerUser.user_id);
                        title = userModel.FullName;
                        break;
                }

                return title;
            }
        }

        public ObservableCollection<MessageModel> Messages {
            get {
                if(messages == null) {
                    messages = new ObservableCollection<MessageModel>();
                    MessagesRequest();
                }

                return messages;
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
                    messagesList = ((Messages_messagesConstructor) loadedMessages).messages;
                    usersList = ((Messages_messagesConstructor) loadedMessages).users;
                    break;
                case Constructor.messages_messagesSlice:
                    chatsList = ((Messages_messagesSliceConstructor) loadedMessages).chats;
                    messagesList = ((Messages_messagesSliceConstructor) loadedMessages).messages;
                    usersList = ((Messages_messagesSliceConstructor) loadedMessages).users;
                    break;
                default:
                    return;
            }


            foreach(var user in usersList) {
                session.SaveUser(new UserModel(user));
            }

            foreach(var chat in chatsList) {
                session.SaveChat(new ChatModel(chat));
            }

            foreach(var message in messagesList) {
                messages.Add(new MessageModel(message));
            }
        }

        public string Preview {
            get {
                string preview = "";
                var topMessage = session.Dialogs.Model.GetMessage(dialog.top_message).RawMessage;

                switch (topMessage.Constructor) {
                    case Constructor.message:
                        preview = ((MessageConstructor)topMessage).message;
                        break;
                    case Constructor.messageForwarded:
                        preview = ((MessageForwardedConstructor)topMessage).message;
                        break;
                    case Constructor.messageService:
                        preview = "SERVICE";
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                return preview;
            }
        }

        public string TimeOrDate {
            get {
                var topMessage = session.Dialogs.Model.GetMessage(dialog.top_message);
                return topMessage.TimeOrDate;
            }
        }

        public void Write(BinaryWriter writer) {
            dialog.Write(writer);
            if(messages == null) {
                writer.Write(0);
            } else {
                int messagesIndexStart = 0;
                int messagesCount = messages.Count;
                if(messagesCount > 100) {
                    messagesCount = 100;
                    messagesIndexStart = messagesCount - 100;
                }
                writer.Write(messagesCount);
                for(int i = messagesIndexStart; i < messagesIndexStart + messagesCount; i++) {
                    messages[i].Write(writer);
                }
            }
        }
    }
}

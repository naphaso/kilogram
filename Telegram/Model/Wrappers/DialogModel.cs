
﻿using System;
﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
﻿using System.ComponentModel;
﻿using System.Diagnostics;
﻿using System.IO.IsolatedStorage;
﻿using System.Linq;
﻿using System.Runtime.CompilerServices;
﻿using System.Threading.Tasks;
﻿using System.Windows;
﻿using System.Windows.Media.Imaging;
﻿using Coding4Fun.Toolkit.Controls.Common;
﻿using Microsoft.Phone.Maps.Controls;
﻿using Microsoft.Phone.Reactive;
﻿using System.IO;
﻿using Telegram.Annotations;
﻿using Telegram.Core.Logging;
﻿using Telegram.MTProto;
﻿using Telegram.MTProto.Components;
﻿using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public class DialogModel: INotifyPropertyChanged {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogModel));
        private DialogConstructor dialog;
        private ObservableCollectionUI<MessageModel> messages;
        private TelegramSession session;
        public event OnNewMessageReceived NewMessageReceived;
        public enum StatusType {
            Static,
            Activity
        }

        public class DialogStatus {
            public StatusType Type { get; set; }
            public string String { get; set; }
        }

        public DialogStatus _currentStatus = new DialogStatus();

        private string MyNamePattern {
            get {
                return "You";
            }
        }

        public DialogModel(Dialog dialog, TelegramSession session, Dictionary<int, MessageModel> messagesMap) {
            this.dialog = (DialogConstructor) dialog;
            this.session = session;
            this.messages = new ObservableCollectionUI<MessageModel>();
            this.messages.Add(messagesMap[this.dialog.top_message]);

            SubscribeToDialog();
        }

        // FIXME: review ctor params !!
        public DialogModel(MessageModelDelivered topMessage, TelegramSession session) {
            this.dialog = (DialogConstructor) TL.dialog(topMessage.Peer, topMessage.Id, 1);
            this.session = session;
            this.messages = new ObservableCollectionUI<MessageModel>();
            this.messages.Add(topMessage);
            
            SubscribeToDialog();
        }

        public DialogModel(TelegramSession session, BinaryReader reader) {
            this.session = session;
            Read(reader);

            SubscribeToDialog();
        }

        private void SubscribeToDialog() {
            switch (dialog.peer.Constructor) {
                case Constructor.peerChat:
                    var peerChat = dialog.peer as PeerChatConstructor;
                    var chat = session.GetChat(peerChat.chat_id);
                    logger.debug("Subscribing PropertyChanged for chat {0}", peerChat);

                    chat.PropertyChanged += DialogOnPropertyChanged;

                    break;
                case Constructor.peerUser:
                    var peerUser = dialog.peer as PeerUserConstructor;
                    var user = session.GetUser(peerUser.user_id);

                    logger.debug("Subscribing PropertyChanged for user {0}", peerUser);

                    user.PropertyChanged += DialogOnPropertyChanged;

                    break;
            }
        }

        // proxy method from holded dialog object (user or chat)
        private void DialogOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
            logger.debug("Property changing [{0}] ", propertyChangedEventArgs.PropertyName);

            if (propertyChangedEventArgs.PropertyName == "Title"
                || propertyChangedEventArgs.PropertyName == "FullName") {
                OnPropertyChanged("Title");
            } else if (propertyChangedEventArgs.PropertyName == "Status") {
                OnPropertyChanged("Status");
                OnPropertyChanged("StatusOrAction");
            } else if (propertyChangedEventArgs.PropertyName == "AvatarPath") {
                logger.debug("Property is AvatarPath");
                OnPropertyChanged("AvatarPath");
            }
        }

        private Dialog RawDialog {
            get {
                return dialog;
            }
        }

        public int Id {
            get {
                switch (dialog.peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = dialog.peer as PeerChatConstructor;
                        return peerChat.chat_id;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        return peerUser.user_id;
                }

                return -1;
            }
        }

        public BitmapImage AvatarPath {
            get {
                if (dialog.peer.Constructor == Constructor.peerChat) {
                    PeerChatConstructor peerChat = (PeerChatConstructor) dialog.peer;
                    ChatModel chat = TelegramSession.Instance.GetChat(peerChat.chat_id);
                    return chat.AvatarPath;
                }

                PeerUserConstructor peerUser = (PeerUserConstructor) dialog.peer;
                UserModel user = TelegramSession.Instance.GetUser(peerUser.user_id);
                return user.AvatarPath;
            }
        }


        public Peer Peer {
            get {
                return dialog.peer;
            }
        }

        private InputPeer InputPeer {
            get {
                if (dialog.peer.Constructor == Constructor.peerChat) {
                    return TL.inputPeerChat(((PeerChatConstructor) dialog.peer).chat_id);
                }

                var peerUser = dialog.peer as PeerUserConstructor;
                var user = session.GetUser(peerUser.user_id);

                return user.InputPeer;
            }
        }

        public DialogStatus PreviewOrAction {
            get {
                if (dialog.peer.Constructor == Constructor.peerUser) {
                    if (userTyping != null) {
                        _currentStatus.String = "typing...";
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        _currentStatus.String = Preview;
                        _currentStatus.Type = StatusType.Static;
                    }
                } else { // peer chat
                    if (chatTyping.Count != 0) {
                        _currentStatus.String = String.Format("{0} users typing...", chatTyping.Count);
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        _currentStatus.String = Preview;
                        _currentStatus.Type = StatusType.Static;
                    }
                }

                return _currentStatus;
            }
        }

        public DialogStatus StatusOrAction {
            get {
                if (dialog.peer.Constructor == Constructor.peerUser) {
                    if (userTyping != null) {
                        _currentStatus.String = "typing...";
                        _currentStatus.Type = StatusType.Activity;
                    } else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                } else { // peer chat
                    if (chatTyping.Count != 0) {
                        _currentStatus.String = String.Format("{0} users typing...", chatTyping.Count);
                        _currentStatus.Type = StatusType.Activity;
                    } else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                }

                return _currentStatus;
            }
        }

        public string Status {
            get {
                string status = "";
                switch (dialog.peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = dialog.peer as PeerChatConstructor;
                        var chat = session.GetChat(peerChat.chat_id);
                        return chat.Status;

                    case Constructor.peerUser:
                        var peerUser = dialog.peer as PeerUserConstructor;
                        var user = session.GetUser(peerUser.user_id);
                        return user.Status;
                }

                return status;
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

        public string Timestamp {
            get {

                MessageModel messageModel = messages.Last();
                if (messageModel.Delivered == false) {
                    return Formatters.FormatDialogDateTimestamp(((MessageModelUndelivered)messageModel).Timestamp);
                }

                string timestamp = "";
                var topMessage = ((MessageModelDelivered)messageModel).RawMessage;

                switch (topMessage.Constructor) {
                    case Constructor.message:
                        timestamp = Formatters.FormatDialogDateTimestampUnix(((MessageConstructor)topMessage).date);
                        break;
                    case Constructor.messageForwarded:
                        timestamp = Formatters.FormatDialogDateTimestampUnix(((MessageForwardedConstructor)topMessage).date);
                        break;
                    case Constructor.messageService:
                        timestamp = Formatters.FormatDialogDateTimestampUnix(((MessageServiceConstructor)topMessage).date);
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                return timestamp;
            }
        }

        public ObservableCollection<MessageModel> Messages {
            get {
                if(messages == null) {
                    messages = new ObservableCollectionUI<MessageModel>();
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
                session.SaveUser(user);
            }

            foreach(var chat in chatsList) {
                session.SaveChat(chat);
            }

            foreach(var message in messagesList) {
                messages.Add(new MessageModelDelivered(message));
            }
        }

        public string Preview {
            get {

                MessageModel messageModel = messages.Last();
                if (messageModel.Delivered == false) {
                    return ((MessageModelUndelivered) messageModel).Text;
                }
                
                string preview = "";
                var topMessage = ((MessageModelDelivered) messageModel).RawMessage;

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

                logger.info("saved {0} messages", messagesCount);
            }
        }

        public void Read(BinaryReader reader) {
            logger.info("loading dialog");
            dialog = new DialogConstructor();
            reader.ReadInt32();
            dialog.Read(reader);
            int messagesCount = reader.ReadInt32();
            logger.info("loading {0} messages", messagesCount);
            messages = new ObservableCollectionUI<MessageModel>();
            for(int i = 0; i < messagesCount; i++) {
                int type = reader.ReadInt32();
                if (type == 1) {
                    // delivered
                    messages.Add(new MessageModelDelivered(reader));
                }
                else {
                    // undelivered
                    messages.Add(new MessageModelUndelivered(reader));
                }
            }

            logger.info("loaded {0} messages", messagesCount);
        }

        public bool IsChat {
            get {
                return dialog.peer.Constructor == Constructor.peerChat;
            }
        }

        public string LastActivityUserName {
            get {
                MessageModel messageModel = messages.Last();

                if (messageModel.Delivered == false) {
                    return MyNamePattern;
                }

                var messageModelDelivered = (MessageModelDelivered) messageModel;

                var topMessage = messageModelDelivered.RawMessage;
                UserModel user = null;
                switch (topMessage.Constructor) {
                    case Constructor.message:
                        user = session.GetUser(((MessageConstructor)topMessage).from_id);
                        break;
                    case Constructor.messageForwarded:
                        user = session.GetUser(((MessageForwardedConstructor)topMessage).from_id);
                        break;
                    case Constructor.messageService:
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
                
                string fullName = "service user";
                
                if (user == null) {
                    return fullName;
                }

                return user.FullName;
            }
        }

        public string Typing {
            get {
                if(dialog.peer.Constructor == Constructor.peerUser) {
                    return userTyping == null ? "" : "Typing...";
                } else {
                    return chatTyping.Count == 0 ? "" : String.Format("{0} users typing...", chatTyping.Count);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                logger.debug("property [{0}] is changed", propertyName);
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ProcessNewMessage(MessageModel messageModel) {
            logger.info("processing message and adding to observable collection");
            messages.Add(messageModel);
            
            if (NewMessageReceived != null)
                NewMessageReceived(this, messageModel);
        }

        public async Task SendMessage(string message) {
            long randomId = Helpers.GenerateRandomLong();

            MessageModelUndelivered undeliveredMessage = new MessageModelUndelivered() {
                MessageType = MessageModelUndelivered.Type.Text,
                Text = message,
                Timestamp = DateTime.Now,
                RandomId = randomId
            };

            ProcessNewMessage(undeliveredMessage);

            messages_SentMessage sentMessage = await TelegramSession.Instance.Api.messages_sendMessage(InputPeer, message, randomId);
            int date, id, pts, seq;
            if(sentMessage.Constructor == Constructor.messages_sentMessage) {
                Messages_sentMessageConstructor sent = (Messages_sentMessageConstructor) sentMessage;
                id = sent.id;
                pts = sent.pts;
                seq = sent.seq;
                date = sent.date;
            } else if(sentMessage.Constructor == Constructor.messages_sentMessageLink) {
                Messages_sentMessageLinkConstructor sent = (Messages_sentMessageLinkConstructor) sentMessage;
                id = sent.id;
                pts = sent.pts;
                seq = sent.seq;
                date = sent.date;
                List<contacts_Link> links = sent.links;
                // TODO: process links
            } else {
                logger.error("unknown sentMessage constructor");
                return;
            }

            int messageIndex = messages.IndexOf(undeliveredMessage);
            if(messageIndex != -1) {
                messages[messageIndex] = new MessageModelDelivered(TL.message(id, session.SelfId, dialog.peer, true, true, date, message, TL.messageMediaEmpty()));
            } else {
                logger.error("not found undelivered message to confirmation");
            }

            session.Updates.processUpdatePtsSeqDate(pts, seq, date);
        }

        public async Task RemoveAndClearDialog() {
            try {
                await ClearDialogHistory();

                if (dialog.peer.Constructor == Constructor.peerChat) {
                    InputPeer peer = InputPeer;
                    InputPeerChatConstructor peerChat = (InputPeerChatConstructor)peer;
                    InputUser user = TL.inputUserSelf();

                    messages_StatedMessage message =
                        await TelegramSession.Instance.Api.messages_deleteChatUser(peerChat.chat_id, user);
                    // TODO: pts and seq
                }

                TelegramSession.Instance.Dialogs.Model.Dialogs.Remove(this);
            }
            catch (Exception ex) {
                logger.error("exception: {0}", ex);
            }
        }

        public async Task ClearDialogHistory() {
            Messages_affectedHistoryConstructor affectedHistory = (Messages_affectedHistoryConstructor)await
                TelegramSession.Instance.Api.messages_deleteHistory(InputPeer, 0);

            // TODO: handle pts and seq
        }

        private UserTyping userTyping;
        private Dictionary<int, UserTyping> chatTyping = new Dictionary<int,UserTyping>();

        private class UserTyping {
            public DateTime lastUpdate;
            public UserTyping(DateTime lastUpdate) {
                this.lastUpdate = lastUpdate;
            }
        }

        public void SetTyping(int userid) {
            logger.debug("user {0} in chat typing in dialog model", userid);
            if(dialog.peer.Constructor == Constructor.peerUser) {
                logger.warning("invalid chat typing event for user dialog");
                return;
            }

            if(chatTyping.ContainsKey(userid)) {
                chatTyping[userid].lastUpdate = DateTime.Now;
            } else {
                chatTyping.Add(userid, new UserTyping(DateTime.Now));
                OnPropertyChanged("PreviewOrAction");
                OnPropertyChanged("StatusOrAction");
            }
        }

        public void SetTyping() {
            logger.debug("user typing in dialog model");
            if(dialog.peer.Constructor == Constructor.peerChat) {
                logger.warning("invalid user typing event for chat dialog");
                return;
            }

            if(userTyping == null) {
                userTyping = new UserTyping(DateTime.Now);
                OnPropertyChanged("PreviewOrAction");
                OnPropertyChanged("StatusOrAction");
            } else {
                userTyping.lastUpdate = DateTime.Now;
            }
        }

        public void UpdateTypings() {
            if(dialog.peer.Constructor == Constructor.peerUser) {
                if(userTyping != null && DateTime.Now - userTyping.lastUpdate > TimeSpan.FromSeconds(5)) {
                    userTyping = null;

                    OnPropertyChanged("PreviewOrAction");
                    OnPropertyChanged("StatusOrAction");
                }
            } else if(dialog.peer.Constructor == Constructor.peerChat) {
                var toRemove = (from typing in chatTyping where DateTime.Now - typing.Value.lastUpdate > TimeSpan.FromSeconds(5) select typing.Key).ToList();

                if(toRemove.Count != 0) {
                    foreach(var i in toRemove) {
                        chatTyping.Remove(i);
                    }

                    OnPropertyChanged("PreviewOrAction");
                    OnPropertyChanged("StatusOrAction");
                }
            }
        }

    }

    public delegate void OnNewMessageReceived(object sender, object args);
}


﻿using System;
﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
﻿using System.Collections.Specialized;
﻿using System.ComponentModel;
﻿using System.Diagnostics;
﻿using System.IO.IsolatedStorage;
﻿using System.Linq;
﻿using System.Runtime.CompilerServices;
﻿using System.Threading.Tasks;
﻿using System.Windows;
﻿using System.Windows.Controls;
﻿using System.Windows.Media.Imaging;
﻿using Coding4Fun.Toolkit.Controls.Common;
﻿using Microsoft.Phone.Controls.Primitives;
﻿using Microsoft.Phone.Maps.Controls;
﻿using Microsoft.Phone.Reactive;
﻿using System.IO;
﻿using Telegram.Annotations;
﻿using Telegram.Core.Logging;
﻿using Telegram.MTProto;
﻿using Telegram.MTProto.Components;
﻿using Telegram.Utils;

namespace Telegram.Model.Wrappers {
    public abstract class DialogModel : INotifyPropertyChanged {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogModel));

        public abstract Peer Peer { get; }
        public abstract string Preview { get; }
        public abstract bool IsChat { get; }
        public abstract bool IsSecret { get; }
        public abstract void Write(BinaryWriter writer);
        public abstract void Read(BinaryReader reader);
        public abstract Task<bool> SendMessage(string message);
        public abstract Task RemoveAndClearDialog();
        public abstract Task ClearDialogHistory();
        public abstract bool IsWaiting { get; }

        public abstract Task SendRead();

        public event PropertyChangedEventHandler PropertyChanged;

        protected TelegramSession session;
        protected ObservableCollectionUI<MessageModel> messages;

        public DialogModel(TelegramSession session) {
            this.session = session;
            this.messages = new ObservableCollectionUI<MessageModel>();
        }

        protected string MyNamePattern {
            get {
                return "You";
            }
        }

        public InputPeer InputPeer {
            get {
                Peer peer = Peer;
                if (peer.Constructor == Constructor.peerChat) {
                    return TL.inputPeerChat(((PeerChatConstructor)peer).chat_id);
                }

                var peerUser = peer as PeerUserConstructor;
                var user = session.GetUser(peerUser.user_id);

                return user.InputPeer;
            }
        }

        public string Status {
            get {
                string status = "";
                Peer peer = Peer;
                switch (peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = peer as PeerChatConstructor;
                        var chat = session.GetChat(peerChat.chat_id);
                        return chat.Status;

                    case Constructor.peerUser:
                        var peerUser = peer as PeerUserConstructor;
                        var user = session.GetUser(peerUser.user_id);
                        return user.Status;
                }

                return status;
            }
        }

        public string Title {
            get {
                string title = "";
                Peer peer = Peer;
                switch (peer.Constructor) {
                    case Constructor.peerChat:
                        var peerChat = peer as PeerChatConstructor;
                        // check null
                        var chatModel = session.GetChat(peerChat.chat_id);
                        title = chatModel.Title;
                        break;

                    case Constructor.peerUser:
                        var peerUser = peer as PeerUserConstructor;
                        var userModel = session.GetUser(peerUser.user_id);
                        title = userModel.FullName;
                        break;
                }

                return title;
            }
        }

        protected string Typing {
            get {
                Peer peer = Peer;

                if (peer.Constructor == Constructor.peerUser) {
                    return userTyping == null ? "" : "typing...";
                } else {
                    if (chatTyping.Count == 0)
                        return "";
                    else if (chatTyping.Count == 1) {
                        UserModel user = session.GetUser(chatTyping.First().Key);
                        return String.Format("{0} is typing...", user.FullName);
                    }
                    return chatTyping.Count == 0 ? "" : String.Format("{0} users typing...", chatTyping.Count);
                }
            }
        }

        public BitmapImage AvatarPath {
            get {
                Peer peer = Peer;
                if (peer.Constructor == Constructor.peerChat) {
                    PeerChatConstructor peerChat = (PeerChatConstructor)peer;
                    ChatModel chat = session.GetChat(peerChat.chat_id);
                    return chat.AvatarPath;
                }

                PeerUserConstructor peerUser = (PeerUserConstructor)peer;
                UserModel user = session.GetUser(peerUser.user_id);
                return user.AvatarPath;
            }
        }

        public MessageModel.MessageDeliveryState MessageDeliveryStateProperty {
            get {
                if (messages.Count == 0)
                    return MessageModel.MessageDeliveryState.NoImage;

                if (messages.Last().IsOut)
                    return messages.Last().MessageDeliveryStateProperty;

                return MessageModel.MessageDeliveryState.NoImage;
            }
        }

        public string LastActivityUserName {
            get {
                MessageModel messageModel = messages.Last();
                return messageModel.Sender.FullName;
            }
        }

        public string Timestamp {
            get {
                if (messages.Count == 0) {
                    return "";
                }

                MessageModel messageModel = messages.Last();
                return Formatters.FormatDialogDateTimestamp(messageModel.Timestamp);
            }
        }

        public bool IsEncrypted {
            get { return this is DialogModelEncrypted; }
        }

        public enum StatusType {
            Static,
            Activity
        }

        public class DialogStatus {
            public StatusType Type { get; set; }
            public string String { get; set; }
        }

        protected UserTyping userTyping;
        protected Dictionary<int, UserTyping> chatTyping = new Dictionary<int, UserTyping>();

        protected class UserTyping {
            public DateTime lastUpdate;
            public UserTyping(DateTime lastUpdate) {
                this.lastUpdate = lastUpdate;
            }
        }

        public ObservableCollection<MessageModel> Messages {
            get {
                return messages;
            }
        }

        public DialogStatus _currentStatus = new DialogStatus();

        public DialogStatus PreviewOrAction {
            get {
                Peer peer = Peer;
                if (peer.Constructor == Constructor.peerUser) {
                    if (userTyping != null) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        return GetPreviewConfig();
                    }
                } else { // peer chat
                    if (chatTyping.Count != 0) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        return GetPreviewConfig();
                    }
                }

                return _currentStatus;
            }
        }

        private DialogStatus GetPreviewConfig() {
            if (messages == null || messages.Count == 0) {
                return new DialogStatus() { String = "new conversation", Type = StatusType.Activity };
            }

            // text, service, media or forwarded

            bool isGreen = false;
            if (messages.Last().MessageDeliveryStateProperty == MessageModel.MessageDeliveryState.Read || messages.Last().IsOut)
                isGreen = false;
            else
                isGreen = true;

            return new DialogStatus() {
                String = Preview,
                Type = isGreen ? StatusType.Activity : StatusType.Static
            };
        }

        public DialogStatus StatusOrAction {
            get {
                Peer peer = Peer;
                if (peer.Constructor == Constructor.peerUser) {
                    if (userTyping != null) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                }
                else { // peer chat
                    if (chatTyping.Count != 0) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    }
                    else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                }

                return _currentStatus;
            }
        }


        public void SetTyping(int userid) {
            logger.debug("user {0} in chat typing in dialog model", userid);
            Peer peer = Peer;
            if (peer.Constructor == Constructor.peerUser) {
                logger.warning("invalid chat typing event for user dialog");
                return;
            }

            if (chatTyping.ContainsKey(userid)) {
                chatTyping[userid].lastUpdate = DateTime.Now;
            } else {
                chatTyping.Add(userid, new UserTyping(DateTime.Now));
                OnPropertyChanged("PreviewOrAction");
                OnPropertyChanged("StatusOrAction");
            }
        }

        public void SetTyping() {
            logger.debug("user typing in dialog model");
            Peer peer = Peer;
            if (peer.Constructor == Constructor.peerChat) {
                logger.warning("invalid user typing event for chat dialog");
                return;
            }

            if (userTyping == null) {
                userTyping = new UserTyping(DateTime.Now);
                OnPropertyChanged("PreviewOrAction");
                OnPropertyChanged("StatusOrAction");
            } else {
                userTyping.lastUpdate = DateTime.Now;
            }
        }

        public virtual void UpdateTypings() {
            Peer peer = Peer;
            if (peer.Constructor == Constructor.peerUser) {
                if (userTyping != null && DateTime.Now - userTyping.lastUpdate > TimeSpan.FromSeconds(5)) {
                    userTyping = null;

                    OnPropertyChanged("PreviewOrAction");
                    OnPropertyChanged("StatusOrAction");
                }
            } else if (peer.Constructor == Constructor.peerChat) {
                var toRemove = (from typing in chatTyping where DateTime.Now - typing.Value.lastUpdate > TimeSpan.FromSeconds(5) select typing.Key).ToList();

                if (toRemove.Count != 0) {
                    foreach (var i in toRemove) {
                        chatTyping.Remove(i);
                    }

                    OnPropertyChanged("PreviewOrAction");
                    OnPropertyChanged("StatusOrAction");
                }
            }
        }

        public virtual void OpenedRead() {
            bool needSendRead = false;
            foreach (var message in from message in messages where !message.IsOut && message.Unread select message) {
                logger.info("message mark read");
                message.MarkRead();
                needSendRead = true;
            }

            if (needSendRead) {
                SendRead();
            }
        }

        public void MarkRead(List<int> messages) {
            var msgs = from msg in this.messages where msg is MessageModelDelivered && messages.Contains(((MessageModelDelivered)msg).Id) select (MessageModelDelivered)msg;
            foreach (var messageModel in msgs) {
                messageModel.SetReadState();
            }

            OnPropertyChanged("MessageDeliveryStateProperty");
            OnPropertyChanged("PreviewOrAction");
            OnPropertyChanged("StatusOrAction");
        }

        public void ProcessNewMessage(MessageModel messageModel) {
            logger.info("processing message and adding to observable collection");
            messages.Add(messageModel);
            Peer peer = Peer;
            if (peer.Constructor == Constructor.peerUser) {
                if (userTyping != null) {
                    userTyping = null;
                }
            } else if (peer.Constructor == Constructor.peerChat) {
                if (chatTyping.Count != 0) {
                    chatTyping.Clear();
                }
            }

            OnPropertyChanged("PreviewOrAction");
            OnPropertyChanged("StatusOrAction");
            OnPropertyChanged("MessageDeliveryStateProperty");
        }

        // proxy method from holded dialog object (user or chat)
        protected void DialogOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
//            logger.debug("Property changing [{0}] ", propertyChangedEventArgs.PropertyName);

            if (propertyChangedEventArgs.PropertyName == "Title"
                || propertyChangedEventArgs.PropertyName == "FullName") {
                OnPropertyChanged("Title");
            } else if (propertyChangedEventArgs.PropertyName == "Status") {
                OnPropertyChanged("Status");
                OnPropertyChanged("StatusOrAction");
            } else if (propertyChangedEventArgs.PropertyName == "AvatarPath") {
//                logger.debug("Property is AvatarPath");
                OnPropertyChanged("AvatarPath");
            } else if (propertyChangedEventArgs.PropertyName == "MessageDeliveryStateProperty") {
                OnPropertyChanged("MessageDeliveryStateProperty");
            }
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        public abstract Task LoadMore();
        public abstract bool LoadMorePossible();

        public abstract void SendTyping(bool typing);
    }
}

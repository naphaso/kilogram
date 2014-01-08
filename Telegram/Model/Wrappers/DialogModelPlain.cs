
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
    class DialogModelPlain : DialogModel {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(DialogModelPlain));
        private DialogConstructor dialog;

        public DialogModelPlain(Dialog dialog, TelegramSession session, Dictionary<int, MessageModel> messagesMap) : base(session) {
            this.dialog = (DialogConstructor) dialog;
            this.messages.Add(messagesMap[this.dialog.top_message]);

            SubscribeToDialog();
        }

        // FIXME: review ctor params !!
        public DialogModelPlain(MessageModelDelivered topMessage, TelegramSession session) : base(session) {
            this.dialog = (DialogConstructor) TL.dialog(topMessage.Peer, topMessage.Id, 1);
            this.messages.Add(topMessage);
            
            SubscribeToDialog();
        }

        public DialogModelPlain(TelegramSession session, BinaryReader reader) : base(session) {
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

        public override BitmapImage AvatarPath {
            get {
                if (dialog.peer.Constructor == Constructor.peerChat) {
                    PeerChatConstructor peerChat = (PeerChatConstructor) dialog.peer;
                    ChatModel chat = session.GetChat(peerChat.chat_id);
                    return chat.AvatarPath;
                }

                PeerUserConstructor peerUser = (PeerUserConstructor) dialog.peer;
                UserModel user = session.GetUser(peerUser.user_id);
                return user.AvatarPath;
            }
        }

        public override Peer Peer {
            get {
                return dialog.peer;
            }
        }

        public override DialogStatus PreviewOrAction {
            get {
                if (dialog.peer.Constructor == Constructor.peerUser) {
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
                return new DialogStatus() {String = "invalid", Type = StatusType.Activity};
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

        public override DialogStatus StatusOrAction {
            get {
                if (dialog.peer.Constructor == Constructor.peerUser) {
                    if (userTyping != null) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    } else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                } else { // peer chat
                    if (chatTyping.Count != 0) {
                        _currentStatus.String = Typing;
                        _currentStatus.Type = StatusType.Activity;
                    } else {
                        _currentStatus.String = Status;
                        _currentStatus.Type = StatusType.Static;
                    }
                }

                return _currentStatus;
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

        public override string Preview {
            get {

                MessageModel messageModel = messages.Last();
                if (messageModel.Delivered == false) {
                    return ((MessageModelUndelivered) messageModel).Text;
                }

                return messageModel.Preview;
            }
        }

        public override void Write(BinaryWriter writer) {
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

        public override void Read(BinaryReader reader) {
            logger.info("loading dialog");
            dialog = new DialogConstructor();
            reader.ReadInt32();
            dialog.Read(reader);
            int messagesCount = reader.ReadInt32();
            logger.info("loading {0} messages", messagesCount);

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

        public override bool IsChat {
            get {
                return dialog.peer.Constructor == Constructor.peerChat;
            }
        }

        public async Task SendMedia(InputMedia media) {
            try {
                long randomId = Helpers.GenerateRandomLong();

                // PHOTO IS HERE
                MessageModelUndelivered undeliveredMessage = new MessageModelUndelivered() {
                    MessageType = MessageModelUndelivered.Type.Text,
                    Text = "",
                    Timestamp = DateTime.Now,
                    RandomId = randomId
                };

                ProcessNewMessage(undeliveredMessage);

                messages_StatedMessage sentMessage =
                    await session.Api.messages_sendMedia(InputPeer, media, randomId);

                Message message;
                int pts, seq, id;
                if (sentMessage.Constructor == Constructor.messages_statedMessage) {
                    Messages_statedMessageConstructor sentMessageConstructor =
        (Messages_statedMessageConstructor)sentMessage;

                    session.Updates.ProcessChats(sentMessageConstructor.chats);
                    session.Updates.ProcessUsers(sentMessageConstructor.users);

                    pts = sentMessageConstructor.pts;
                    seq = sentMessageConstructor.seq;
                    message = sentMessageConstructor.message;
                    
                } else if (sentMessage.Constructor == Constructor.messages_statedMessageLink) {
                    Messages_statedMessageLinkConstructor statedMessageLink =
                        (Messages_statedMessageLinkConstructor) sentMessage;

                    session.Updates.ProcessChats(statedMessageLink.chats);
                    session.Updates.ProcessUsers(statedMessageLink.users);
                    // TODO: process links

                    pts = statedMessageLink.pts;
                    seq = statedMessageLink.seq;
                    message = statedMessageLink.message;
                }
                else {
                    logger.error("unknown messages_StatedMessage constructor");
                    return;
                }

                if (!session.Updates.processUpdatePtsSeq(pts, seq)) {
                    return;
                }

                int messageIndex = messages.IndexOf(undeliveredMessage);
                if (messageIndex != -1) {
                    messages[messageIndex] =
                        new MessageModelDelivered(message);
                } else {
                    logger.error("not found undelivered message to confirmation");
                }

            }
            catch (Exception ex) {
                logger.error("Error sending media {0}", ex);
            }
        }

        public override async Task SendMessage(string message) {
            try {
                long randomId = Helpers.GenerateRandomLong();

                MessageModelUndelivered undeliveredMessage = new MessageModelUndelivered() {
                    MessageType = MessageModelUndelivered.Type.Text,
                    Text = message,
                    Timestamp = DateTime.Now,
                    RandomId = randomId
                };

                ProcessNewMessage(undeliveredMessage);

                // TODO: npe? 
                messages_SentMessage sentMessage =
                    await session.Api.messages_sendMessage(InputPeer, message, randomId);
                int date, id, pts, seq;
                if (sentMessage.Constructor == Constructor.messages_sentMessage) {
                    Messages_sentMessageConstructor sent = (Messages_sentMessageConstructor) sentMessage;
                    id = sent.id;
                    pts = sent.pts;
                    seq = sent.seq;
                    date = sent.date;
                }
                else if (sentMessage.Constructor == Constructor.messages_sentMessageLink) {
                    Messages_sentMessageLinkConstructor sent = (Messages_sentMessageLinkConstructor) sentMessage;
                    id = sent.id;
                    pts = sent.pts;
                    seq = sent.seq;
                    date = sent.date;
                    List<contacts_Link> links = sent.links;
                    // TODO: process links
                }
                else {
                    logger.error("unknown sentMessage constructor");
                    return;
                }

                int messageIndex = messages.IndexOf(undeliveredMessage);
                if (messageIndex != -1) {
                    messages[messageIndex] =
                        new MessageModelDelivered(TL.message(id, session.SelfId, dialog.peer, true, true, date, message,
                            TL.messageMediaEmpty()));
                }
                else {
                    logger.error("not found undelivered message to confirmation");
                }

                session.Updates.processUpdatePtsSeqDate(pts, seq, date);
            }
            catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        public override async Task RemoveAndClearDialog() {
            try {
                await ClearDialogHistory();

                if (dialog.peer.Constructor == Constructor.peerChat) {
                    InputPeer peer = InputPeer;
                    InputPeerChatConstructor peerChat = (InputPeerChatConstructor)peer;
                    InputUser user = TL.inputUserSelf();

                    messages_StatedMessage message =
                        await session.Api.messages_deleteChatUser(peerChat.chat_id, user);

                    switch (message.Constructor) {
                        case Constructor.messages_statedMessage:
                            session.Updates.processUpdatePtsSeq( ((Messages_statedMessageConstructor) message).pts, ((Messages_statedMessageConstructor)message).seq);
                            break;

                        case Constructor.messages_statedMessageLink:
                            session.Updates.processUpdatePtsSeq(((Messages_statedMessageLinkConstructor)message).pts, ((Messages_statedMessageLinkConstructor)message).seq);
                            break;
                    }
                }

                session.Dialogs.Model.Dialogs.Remove(this);
            }
            catch (Exception ex) {
                logger.error("exception: {0}", ex);
            }
        }

        public override async Task ClearDialogHistory() {
            Messages_affectedHistoryConstructor affectedHistory = (Messages_affectedHistoryConstructor) await
                session.Api.messages_deleteHistory(InputPeer, 0);

            session.Updates.processUpdatePtsSeq(affectedHistory.pts, affectedHistory.seq);
        }
    }    
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Xna.Framework.GamerServices;
using Telegram.Core;
using Telegram.Core.Logging;

namespace Telegram.MTProto.Components {
    public delegate void NewMessageHandler(Message message);

    public delegate void UserStatusHandler(int userId, UserStatus status);

    public delegate void UserTypingHandler(int userId);

    public delegate void ChatTypingHandler(int chatId, int userId);

    public delegate void UserNameHandler(int userId, string firstName, string lastName);

    public delegate void UserPhotoHandler(int userId, int date, UserProfilePhoto photo, bool previous);

    public delegate void MessagesReadHandler(List<int> messages);

    public delegate void EncryptedChatHandler(EncryptedChat chat);

    public delegate void NewEncryptedMessageHandler(EncryptedMessage message);

    public delegate void EncryptedReadHandler(int chatId, int maxDate, int date);

    public class UpdatesProcessor {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(UpdatesProcessor));
        private TelegramSession session;

        public event NewMessageHandler NewMessageEvent;
        public event UserStatusHandler UserStatusEvent;
        public event UserTypingHandler UserTypingEvent;
        public event ChatTypingHandler ChatTypingEvent;
        public event UserNameHandler UserNameEvent;
        public event UserPhotoHandler UserPhotoEvent;
        public event MessagesReadHandler MessagesReadEvent;
        public event EncryptedChatHandler EncryptedChatEvent;
        public event NewEncryptedMessageHandler EncryptedMessageEvent;
        public event EncryptedReadHandler EncryptedReadEvent;

        // update state
        private int pts;
        private int qts;
        private int date;
        private int seq;
        //private int unread_count; // needed? 
        //private int friends_unread_count;  // needed?

        public UpdatesProcessor(TelegramSession session) {
            this.session = session;
            InitDifferenceExecutor();
        }

        public UpdatesProcessor(TelegramSession session, BinaryReader reader) : this(session) {
            Read(reader);
        }

        public int Date {
            get { return date; }
        }


        private void InitDifferenceExecutor() {
            DifferenceExecutor = new RequestTask(async delegate {
                await session.Established;
                logger.info("get difference from state: pts {0}, qts {1}, date {2}", pts, qts, date);
                updates_Difference difference = await this.session.Api.updates_getDifference(pts, date, qts);
                while (difference.Constructor == Constructor.updates_differenceSlice || difference.Constructor == Constructor.updates_difference) {
                    List<Message> new_messages;
                    List<Update> other_updates;
                    List<Chat> chats;
                    List<User> users;
                    Updates_stateConstructor state;

                    logger.debug("processing difference: {0}", difference);

                    if (difference.Constructor == Constructor.updates_difference) {
                        Updates_differenceConstructor diff = (Updates_differenceConstructor)difference;
                        new_messages = diff.new_messages;
                        other_updates = diff.other_updates;
                        chats = diff.chats;
                        users = diff.users;
                        state = (Updates_stateConstructor)diff.state;

                        ProcessUpdatesDifference(new_messages, other_updates, chats, users, state);

                        break;
                    }
                    else {
                        Updates_differenceSliceConstructor diff = (Updates_differenceSliceConstructor)difference;
                        new_messages = diff.new_messages;
                        other_updates = diff.other_updates;
                        chats = diff.chats;
                        users = diff.users;
                        state = (Updates_stateConstructor)diff.intermediate_state;

                        ProcessUpdatesDifference(new_messages, other_updates, chats, users, state);

                        difference = await this.session.Api.updates_getDifference(pts, date, qts);
                    }
                }

                logger.info("get difference completed successfully");
            });
        }

        private void ProcessUpdatesDifference(List<Message> new_messages, List<Update> other_updates, List<Chat> chats, List<User> users, Updates_stateConstructor state) {
            ProcessUsers(users);
            ProcessChats(chats);
            ProcessNewMessages(new_messages);
            ProcessUpdates(other_updates);

            lock(this) {
                logger.debug("saving new updates state: pts {0}, qts {1}, seq {2}, date {3}", state.pts, state.qts, state.seq, state.date);

                pts = state.pts;
                qts = state.qts;
                date = state.date;
                seq = state.seq;
                // TODO: unread count
            }
        }

        public void ProcessUsers(List<User> users) {
            foreach(var user in users) {
                session.SaveUser(user);
            }
        }

        public void ProcessChats(List<Chat> chats) {
            foreach(var chat in chats) {
                session.SaveChat(chat);
            }
        }

        private void ProcessNewMessages(List<Message> messages) {
            foreach(var message in messages) {
                ProcessNewMessage(message);
            }
        }

        private void ProcessNewMessage(Message message) {
            NewMessageEvent(message);
        }


        // retreiving state
        public async Task GetStateRequest() {
            Updates_stateConstructor state = (Updates_stateConstructor)await session.Api.updates_getState();
            lock (this) {
                logger.debug("setting update state: pts {0}, qts {1}, seq {2}, date {3}", state.pts, state.qts, state.seq, state.date);
                this.pts = state.pts;
                this.qts = state.qts;
                this.seq = state.seq;
                this.date = state.date;
            }
        }

        // update request
        private RequestTask DifferenceExecutor;

        // save and load
        public void Write(BinaryWriter writer) {
            lock(this) {
                writer.Write(pts);
                writer.Write(qts);
                writer.Write(date);
                writer.Write(seq);
                logger.debug("saved updates state: pts {0}, qts {1}, date {2}, seq {3}", pts, qts, date, seq);
            }
        }

        public void Read(BinaryReader reader) {
            lock(this) {
                pts = reader.ReadInt32();
                qts = reader.ReadInt32();
                date = reader.ReadInt32();
                seq = reader.ReadInt32();
                logger.debug("loaded updates state: pts {0}, qts {1}, date {2}, seq {3}", pts, qts, date, seq);
            }
        }

        // update numbers processing methods
        private void updatePts(int pts) {
            logger.info("update pts from {0} to {1}", this.pts, pts);
            lock(this) {
                this.pts = pts;    
            }
        }

        private bool updateSeq(int seq) {
            lock(this) {
                logger.info("update seq from {0} to {1}", this.seq, seq);
                if(seq == 0) {
                    return true;
                }

                if(seq <= this.seq) {
                    logger.debug("update alteady taken, skip");
                    return false;
                }

                if(seq - this.seq > 1) {
                    logger.warning("lost updates! skip and force get difference");
                    DifferenceExecutor.Request();
                    return false;
                }

                logger.info("regular update");

                this.seq = seq;
                return true;
            }
        }

        private bool updateSeq(int startSeq, int seq) {
            lock(this) {
                logger.info("update seq combined: stored seq {0}, start seq {1}, new seq {2}", this.seq, startSeq, seq);

                if(seq == 0) {
                    return true;
                }

                if(seq <= this.seq) {
                    logger.info("update already taken, skip");
                    return false;
                }

                if(startSeq - this.seq > 1) {
                    logger.warning("lost updates! skip and force get difference");
                    DifferenceExecutor.Request();
                    return false;
                }

                logger.info("regular combined update");
                this.seq = seq;
                return true;
            }
        }

        public bool processUpdatePtsSeq(int pts, int seq) {
            lock(this) {
                if(updateSeq(seq)) {
                    this.pts = pts;
                    return true;
                } else {
                    return false;
                }
            }
        }

        public bool processUpdatePtsSeqDate(int pts, int seq, int date) {
            lock(this) {
                if(updateSeq(seq)) {
                    this.pts = pts;
                    this.date = date;
                    return true;
                } else {
                    return false;
                }

            }
        }

        public bool processUpdateSeqDate(int seq, int date) {
            lock(this) {
                if(updateSeq(seq)) {
                    this.date = date;
                    return true;
                } else {
                    return false;
                }
            }
        }

        public bool processUpdateDate(int date) {
            lock(this) {
                if(this.date <= date) {
                    this.date = date;
                    return true;
                } else {
                    return false;
                }
            }
        }


        public bool processUpdateSeqRangeDate(int seqStart, int seq, int date) {
            lock(this) {
                if(updateSeq(seqStart, seq)) {
                    this.date = date;
                    return true;
                } else {
                    return false;
                }
            }
        }


        public void ProcessUpdates(Updates update) {
            logger.info("processing updates: {0}", update);
            switch (update.Constructor) {
                case Constructor.updatesTooLong:
                    ProcessUpdate((UpdatesTooLongConstructor)update);
                    break;
                case Constructor.updateShortMessage:
                    ProcessUpdate((UpdateShortMessageConstructor)update);
                    break;
                case Constructor.updateShortChatMessage:
                    ProcessUpdate((UpdateShortChatMessageConstructor)update);
                    break;
                case Constructor.updateShort:
                    ProcessUpdate((UpdateShortConstructor)update);
                    break;
                case Constructor.updatesCombined:
                    ProcessUpdate((UpdatesCombinedConstructor)update);
                    break;
                case Constructor.updates:
                    ProcessUpdate((UpdatesConstructor)update);
                    break;
            }
        }

        private void ProcessUpdate(UpdatesTooLongConstructor update) {
            logger.debug("updates too long, force get difference");
            DifferenceExecutor.Request();
        }

        private void ProcessUpdate(UpdateShortMessageConstructor update) {
            logger.info("processing short message: {0}", update);
            if(!processUpdatePtsSeqDate(update.pts, update.seq, update.date)) {
                return;
            }

            Message message = TL.message(update.id, update.from_id, session.SelfPeer, false, true, update.date, update.message, TL.messageMediaEmpty());
            NewMessageEvent(message);
        }

        private void ProcessUpdate(UpdateShortChatMessageConstructor update) {
            logger.info("processing short chat message: {0}", update);
            if(!processUpdatePtsSeqDate(update.pts, update.seq, update.date)) {
                return;
            }

            Message message = TL.message(update.id, update.from_id, TL.peerChat(update.chat_id), false, true, update.date, update.message, TL.messageMediaEmpty());
            NewMessageEvent(message);
        }

        private void ProcessUpdate(UpdateShortConstructor update) {
            this.date = update.date;

            ProcessUpdate(update.update);
        }



        private void ProcessUpdate(UpdatesCombinedConstructor update) {
            if(!processUpdateSeqRangeDate(update.seq_start, update.seq, date)) {
                return;
            }

            ProcessUsers(update.users);
            ProcessChats(update.chats);
            
            foreach(var innerUpdate in update.updates) {
                ProcessUpdate(innerUpdate);
            }
        }

        private void ProcessUpdate(UpdatesConstructor update) {
            if(!processUpdateSeqDate(update.seq, update.date)) {
                return;
            }

            ProcessUsers(update.users);
            ProcessChats(update.chats);

            ProcessUpdates(update.updates);
        }

        private void ProcessUpdates(List<Update> updates) {
            foreach (var innerUpdate in updates) {
                ProcessUpdate(innerUpdate);
            }
        }

        // update processing

        private void ProcessUpdate(Update update) {
            switch(update.Constructor) {
                case Constructor.updateNewMessage:
                    ProcessUpdate((UpdateNewMessageConstructor)update);
                    break;
                case Constructor.updateMessageID:
                    ProcessUpdate((UpdateMessageIDConstructor)update);
                    break;
                case Constructor.updateReadMessages:
                    ProcessUpdate((UpdateReadMessagesConstructor)update);
                    break;
                case Constructor.updateDeleteMessages:
                    ProcessUpdate((UpdateDeleteMessagesConstructor)update);
                    break;
                case Constructor.updateRestoreMessages:
                    ProcessUpdate((UpdateRestoreMessagesConstructor)update);
                    break;
                case Constructor.updateUserTyping:
                    ProcessUpdate((UpdateUserTypingConstructor)update);
                    break;
                case Constructor.updateChatUserTyping:
                    ProcessUpdate((UpdateChatUserTypingConstructor)update);
                    break;
                case Constructor.updateChatParticipants:
                    ProcessUpdate((UpdateChatParticipantsConstructor)update);
                    break;
                case Constructor.updateUserStatus:
                    ProcessUpdate((UpdateUserStatusConstructor)update);
                    break;
                case Constructor.updateUserName:
                    ProcessUpdate((UpdateUserNameConstructor)update);
                    break;
                case Constructor.updateUserPhoto:
                    ProcessUpdate((UpdateUserPhotoConstructor)update);
                    break;
                case Constructor.updateContactRegistered:
                    ProcessUpdate((UpdateContactRegisteredConstructor)update);
                    break;
                case Constructor.updateContactLink:
                    ProcessUpdate((UpdateContactLinkConstructor)update);
                    break;
                case Constructor.updateActivation:
                    ProcessUpdate((UpdateActivationConstructor)update);
                    break;
                case Constructor.updateNewAuthorization:
                    ProcessUpdate((UpdateNewAuthorizationConstructor)update);
                    break;
                case Constructor.updateNewGeoChatMessage:
                    ProcessUpdate((UpdateNewGeoChatMessageConstructor)update);
                    break;
                case Constructor.updateNewEncryptedMessage:
                    ProcessUpdate((UpdateNewEncryptedMessageConstructor)update);
                    break;
                case Constructor.updateEncryptedChatTyping:
                    ProcessUpdate((UpdateEncryptedChatTypingConstructor)update);
                    break;
                case Constructor.updateEncryption:
                    ProcessUpdate((UpdateEncryptionConstructor)update);
                    break;
                case Constructor.updateEncryptedMessagesRead:
                    ProcessUpdate((UpdateEncryptedMessagesReadConstructor)update);
                    break;
                case Constructor.updateChatParticipantAdd:
                    ProcessUpdate((UpdateChatParticipantAddConstructor)update);
                    break;
                case Constructor.updateChatParticipantDelete:
                    ProcessUpdate((UpdateChatParticipantDeleteConstructor)update);
                    break;
                case Constructor.updateDcOptions:
                    ProcessUpdate((UpdateDcOptionsConstructor)update);
                    break;
            }
        }

        private void ProcessUpdate(UpdateNewMessageConstructor update) {
            updatePts(update.pts);
            NewMessageEvent(update.message);
        }

        private void ProcessUpdate(UpdateMessageIDConstructor update) {
            
        }

        private void ProcessUpdate(UpdateReadMessagesConstructor update) {
            updatePts(update.pts);
            MessagesReadEvent(update.messages);
        }
        private void ProcessUpdate(UpdateDeleteMessagesConstructor update) {
            updatePts(update.pts);
        }
        private void ProcessUpdate(UpdateRestoreMessagesConstructor update) {
            updatePts(update.pts);

        }
        private void ProcessUpdate(UpdateUserTypingConstructor update) {
            logger.debug("user typing update");
            UserTypingEvent(update.user_id);
        }
        private void ProcessUpdate(UpdateChatUserTypingConstructor update) {
            logger.debug("chat typing update");
            ChatTypingEvent(update.chat_id, update.user_id);
        }
        private void ProcessUpdate(UpdateChatParticipantsConstructor update) {
            
        }
        private void ProcessUpdate(UpdateUserStatusConstructor update) {
            UserStatusEvent(update.user_id, update.status);
        }

        private void ProcessUpdate(UpdateUserNameConstructor update) {
            UserNameEvent(update.user_id, update.first_name, update.last_name);
        }

        private void ProcessUpdate(UpdateUserPhotoConstructor update) {
            UserPhotoEvent(update.user_id, update.date, update.photo, update.previous);
        }

        private void ProcessUpdate(UpdateContactRegisteredConstructor update) {
            
        }

        private void ProcessUpdate(UpdateContactLinkConstructor update) {
            
        }
        private void ProcessUpdate(UpdateActivationConstructor update) {
            
        }
        private void ProcessUpdate(UpdateNewAuthorizationConstructor update) {

        }
        private void ProcessUpdate(UpdateNewGeoChatMessageConstructor update) {
            
        }
        private void ProcessUpdate(UpdateNewEncryptedMessageConstructor update) {
            qts = update.qts;
            EncryptedMessageEvent(update.message);

        }
        private void ProcessUpdate(UpdateEncryptedChatTypingConstructor update) {
            
        }
        private void ProcessUpdate(UpdateEncryptionConstructor update) {
            logger.info("process update encryption");
            processUpdateDate(update.date);
            EncryptedChatEvent(update.chat);
        }
        private void ProcessUpdate(UpdateEncryptedMessagesReadConstructor update) {
            processUpdateDate(update.date);
            EncryptedReadEvent(update.chat_id, update.max_date, update.date);
        }
        private void ProcessUpdate(UpdateChatParticipantAddConstructor update) {
            
        }
        private void ProcessUpdate(UpdateChatParticipantDeleteConstructor update) {
            
        }
        private void ProcessUpdate(UpdateDcOptionsConstructor update) {
            
        }


        public void RequestDifference() {
            if (TelegramSession.Instance.AuthorizationExists())// && TelegramSession.Instance.Connected) 
                DifferenceExecutor.Request();
        }

        private void Process(List<contacts_Link> links) {
            logger.info("need process links!");
        }

        public void Process(messages_StatedMessage statedMessage) {
            switch(statedMessage.Constructor) {
                case Constructor.messages_statedMessage:
                    Process((Messages_statedMessageConstructor)statedMessage);
                    break;
                case Constructor.messages_statedMessageLink:
                    Process((Messages_statedMessageLinkConstructor)statedMessage);
                    break;
            }
        }

        private void Process(Messages_statedMessageConstructor statedMessage) {
            if(!processUpdatePtsSeq(statedMessage.pts, statedMessage.seq)) {
                return;
            }

            ProcessUsers(statedMessage.users);
            ProcessChats(statedMessage.chats);

            ProcessNewMessage(statedMessage.message);
        }

        private void Process(Messages_statedMessageLinkConstructor statedMessage) {
            if (!processUpdatePtsSeq(statedMessage.pts, statedMessage.seq)) {
                return;
            }

            ProcessUsers(statedMessage.users);
            ProcessChats(statedMessage.chats);
            Process(statedMessage.links);

            ProcessNewMessage(statedMessage.message);
        }

        public void Process(messages_StatedMessages messages) {
            switch(messages.Constructor) {
                case Constructor.messages_statedMessages:
                    Process((Messages_statedMessagesConstructor)messages);
                    break;
                case Constructor.messages_statedMessagesLinks:
                    Process((Messages_statedMessagesLinksConstructor)messages);
                    break;
            }
        }

        private void Process(Messages_statedMessagesConstructor messages) {
            if(!processUpdatePtsSeq(messages.pts, messages.seq)) {
                return;
            }

            ProcessUsers(messages.users);
            ProcessChats(messages.chats);

            ProcessNewMessages(messages.messages);
        }

        private void Process(Messages_statedMessagesLinksConstructor messages) {
            if (!processUpdatePtsSeq(messages.pts, messages.seq)) {
                return;
            }

            ProcessUsers(messages.users);
            ProcessChats(messages.chats);
            Process(messages.links);

            ProcessNewMessages(messages.messages);
        }
    }
}

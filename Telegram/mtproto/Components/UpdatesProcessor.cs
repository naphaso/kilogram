using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Core.Logging;

namespace Telegram.MTProto.Components {
    public delegate void NewMessageHandler(Message message);

    public class UpdatesProcessor {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(UpdatesProcessor));
        private TelegramSession session;

        public event NewMessageHandler NewMessageEvent;

        public UpdatesProcessor(TelegramSession session) {
            this.session = session;
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
            // process to long updates
        }

        private void ProcessUpdate(UpdateShortMessageConstructor update) {
            logger.info("processing short message: {0}", update);
            // TODO: process pts, seq
            Message message = TL.message(update.id, update.from_id, session.SelfPeer, false, true, update.date, update.message, TL.messageMediaEmpty());
            NewMessageEvent(message);
        }

        private void ProcessUpdate(UpdateShortChatMessageConstructor update) {
            // TODO: process pts, seq
            Message message = TL.message(update.id, update.from_id, TL.peerChat(update.chat_id), false, true, update.date, update.message, TL.messageMediaEmpty());
            NewMessageEvent(message);
        }

        private void ProcessUpdate(UpdateShortConstructor update) {
            // TODO: work with date
            ProcessUpdate(update.update);
        }



        private void ProcessUpdate(UpdatesCombinedConstructor update) {
            // TODO: process users,chats,date, seq_start, seq_end
            foreach(var innerUpdate in update.updates) {
                ProcessUpdate(innerUpdate);
            }
        }

        private void ProcessUpdate(UpdatesConstructor update) {
            // TODO: process users, chats, date, seq
            foreach(var innerUpdate in update.updates) {
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
            // TODO: process pts
            NewMessageEvent(update.message);
        }

        private void ProcessUpdate(UpdateMessageIDConstructor update) {
            
        }

        private void ProcessUpdate(UpdateReadMessagesConstructor update) {

        }
        private void ProcessUpdate(UpdateDeleteMessagesConstructor update) {

        }
        private void ProcessUpdate(UpdateRestoreMessagesConstructor update) {

        }
        private void ProcessUpdate(UpdateUserTypingConstructor update) {

        }
        private void ProcessUpdate(UpdateChatUserTypingConstructor update) {

        }
        private void ProcessUpdate(UpdateChatParticipantsConstructor update) {

        }
        private void ProcessUpdate(UpdateUserStatusConstructor update) {

        }

        private void ProcessUpdate(UpdateUserNameConstructor update) {

        }

        private void ProcessUpdate(UpdateUserPhotoConstructor update) {

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

        }
        private void ProcessUpdate(UpdateEncryptedChatTypingConstructor update) {

        }
        private void ProcessUpdate(UpdateEncryptionConstructor update) {

        }
        private void ProcessUpdate(UpdateEncryptedMessagesReadConstructor update) {

        }
        private void ProcessUpdate(UpdateChatParticipantAddConstructor update) {

        }
        private void ProcessUpdate(UpdateChatParticipantDeleteConstructor update) {

        }
        private void ProcessUpdate(UpdateDcOptionsConstructor update) {

        }


    }
}

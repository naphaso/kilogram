using System.Collections.Generic;
using Telegram.MTProto;


namespace Telegram.Model.TLWrappers {
    class DialogModel {
        private DialogConstructor dialog;
        private IMessageProvider messageProvider;
        private IUserProvider userProvider;
        private IChatProvider chatProvider;

        public DialogModel(Dialog dialog, IMessageProvider messageProvider, IUserProvider userProvider, IChatProvider chatProvider) {
            this.dialog = (DialogConstructor) dialog;
            this.messageProvider = messageProvider;
            this.userProvider = userProvider;
            this.chatProvider = chatProvider;
        }

        public Dialog RawDialog {
            get {
                return dialog;
            }
        }
    }
}

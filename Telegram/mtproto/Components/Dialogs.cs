using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Annotations;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;

namespace Telegram.MTProto.Components {

    public class Dialogs {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Dialogs));
        private TelegramSession session;
        private DialogListModel model;
 
        public Dialogs(TelegramSession session) {
            this.session = session;
            model = new DialogListModel(session);
        }

        public Dialogs(TelegramSession session, BinaryReader reader) {
            this.session = session;
            load(reader);
        }

        public async Task DialogsRequest() { // call it only on login!
            DialogListModel newState = new DialogListModel(session);

            int offset = 0;
            while(true) {
                logger.info("request dialogs with offset {0}", offset);
                messages_Dialogs dialogsPart = await session.Api.messages_getDialogs(offset, 0, 100);
                offset += newState.ProcessDialogs(dialogsPart);

                if(dialogsPart.Constructor == Constructor.messages_dialogs) {
                    break;
                }
            }

            //model.Replace(newState);
            model = newState;
        }

        public DialogListModel Model {
            get {
                return model;
            }
        }

        public void save(BinaryWriter writer) {
            if(model == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                model.save(writer);
            }
        }

        public void load(BinaryReader reader) {
            logger.info("loading dialogs");
            int stateExists = reader.ReadInt32();
            if(stateExists != 0) {
                logger.info("dialog model found");
                model = new DialogListModel(session);
                model.load(reader);
            } else {
                logger.info("dialogs model not found");
                model = null;
            }
        }
    }

}

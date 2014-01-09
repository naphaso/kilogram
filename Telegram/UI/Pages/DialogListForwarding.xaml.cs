using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.Utils;

namespace Telegram.UI.Pages {
    public partial class DialogListForwarding : PhoneApplicationPage {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(MessageModelDelivered));

        public DialogListForwarding() {
            InitializeComponent();

        }

        private int fromMedia = 1;
        private List<int> messageId = new List<int>();
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string uriParam = "";

            if (NavigationContext.QueryString.TryGetValue("messageId", out uriParam)) {
                messageId.Add(int.Parse(uriParam));
            } else if (NavigationContext.QueryString.TryGetValue("fromMeida", out uriParam)) {
                fromMedia = int.Parse(uriParam);
            }
        }

        private void OnDialogSelected(object sender, DialogModel model) {
            InputPeer inputPeer = null;

            if (!(model is DialogModelPlain)) {
                return;
            }

            DialogModelPlain dmp = (DialogModelPlain) model;

            if (fromMedia == 1) {

                MessageMedia media = MediaTransitionHelper.Instance.Media;

                InputMedia im = null;
                if (media.Constructor == Constructor.messageMediaPhoto) {
                    MessageMediaPhotoConstructor mmpc = (MessageMediaPhotoConstructor) media;
                    InputPhoto ip = TL.inputPhoto(((PhotoConstructor) mmpc.photo).id, ((PhotoConstructor) mmpc.photo).access_hash);

                    im = TL.inputMediaPhoto(ip);
                } else if (media.Constructor == Constructor.messageMediaVideo) {
                    MessageMediaVideoConstructor mmvc = (MessageMediaVideoConstructor) media;
                    InputVideo iv = TL.inputVideo(((VideoConstructor)mmvc.video).id, ((VideoConstructor)mmvc.video).access_hash);

                    im = TL.inputMediaVideo(iv);
                } else if (media.Constructor == Constructor.messageMediaGeo) {
                    MessageMediaGeoConstructor mmgc = (MessageMediaGeoConstructor) media;
                    GeoPointConstructor gpc = (GeoPointConstructor) mmgc.geo;

                    InputGeoPoint point = TL.inputGeoPoint(gpc.lat, gpc.lng);
                    im = TL.inputMediaGeoPoint(point);
                }

                if (im != null) {
                    dmp.SendMedia(im);
                    NavigationService.GoBack();
                }
                 
                return;
            }

            if (messageId.Count == 0) {
                logger.error("error forwarding, no messageId");
            }

            Peer peer = model.Peer;

            if (model.IsChat)
                inputPeer = TL.inputPeerChat(((PeerChatConstructor) peer).chat_id);
            else
                inputPeer = TL.inputPeerContact(((PeerUserConstructor) peer).user_id);

            DoForwardMessages(inputPeer);

            int modelId = TelegramSession.Instance.Dialogs.Model.Dialogs.IndexOf(model);
            NavigationService.Navigate(new Uri("/UI/Pages/DialogPage.xaml?modelId=" + modelId, UriKind.Relative));
        }

        private async Task DoForwardMessages(InputPeer peer) {
            if (messageId.Count == 1) {
                messages_StatedMessage msg = await TelegramSession.Instance.Api.messages_forwardMessage(peer, messageId.First(),
                    Helpers.GenerateRandomLong());

                TelegramSession.Instance.Updates.Process(msg);
            }
            else {
                messages_StatedMessages msgs = await TelegramSession.Instance.Api.messages_forwardMessages(peer, messageId);

                TelegramSession.Instance.Updates.Process(msgs);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Info;
using Microsoft.Phone.Notification;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.UI;
using Telegram.UI.Controls;

namespace Telegram.Notifications {
    public class NotificationManager {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(NotificationManager));
        private static string ChannelName = "UndefwareTelegram";
        private static string AppVersion = "1.0";

        public async Task UnregisterPushNotifications() {
            try {
                var pushChannel = HttpNotificationChannel.Find(ChannelName);

                if (pushChannel != null) {
                    bool register =
                        await
                            TelegramSession.Instance.Api.account_unregisterDevice(3, pushChannel.ChannelUri.ToString());
                }
            }
            catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        public async Task RegisterPushNotifications() {
            try {
                await TelegramSession.Instance.Established;

                var pushChannel = HttpNotificationChannel.Find(ChannelName);

                if (pushChannel == null) {
                    pushChannel = new HttpNotificationChannel(ChannelName);

                    // Register for all the events before attempting to open the channel.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                    // Register for this notification only if you need to receive the notifications while your application is running.
                    pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);
                    pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(HttpNotificationReceived);

                    pushChannel.Open();

                    // Bind this new channel for toast events.
                    pushChannel.BindToShellToast();
                    
                    bool register = await
    TelegramSession.Instance.Api.account_registerDevice(3, pushChannel.ChannelUri.ToString(), DeviceStatus.DeviceName,
        Environment.OSVersion.ToString(), AppVersion, true, "ru");
                    logger.debug("Registering GCM result {0}", register.ToString());
                } else {
                    bool register = await TelegramSession.Instance.Api.account_registerDevice(3, pushChannel.ChannelUri.ToString(), DeviceStatus.DeviceName,
    Environment.OSVersion.ToString(), AppVersion, true, "ru");
                    logger.debug("Registering GCM result {0}", register.ToString());

                    // The channel was already open, so just register for all the events.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                    // Register for this notification only if you need to receive the notifications while your application is running.
                    pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                    pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(HttpNotificationReceived);
                    
                    // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                    System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                    logger.debug(String.Format("Channel Uri is {0}",
                        pushChannel.ChannelUri.ToString()));

                }

            }
            catch (Exception ex) {
                logger.error("exception {0}", ex);
            }
        }

        private void HttpNotificationReceived(object sender, HttpNotificationEventArgs httpNotificationEventArgs) {
            logger.debug("HTTP notification: {0} with headers {1}", httpNotificationEventArgs.Notification.Body, httpNotificationEventArgs.Notification.Headers);
        }

        /// <summary>
        /// Event handler for when the push channel Uri is updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e) {

//            Dispatcher.BeginInvoke(() => {
//                // Display the new URI for testing purposes.   Normally, the URI would be passed back to your web service at this point.
//                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
//                MessageBox.Show(String.Format("Channel Uri is {0}",
//                    e.ChannelUri.ToString()));
//
//            });
            logger.debug(String.Format("Channel Uri is {0}",
                    e.ChannelUri.ToString()));

        }

        /// <summary>
        /// Event handler for when a push notification error occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e) {
            // Error handling logic for your particular application would be here.
//            Dispatcher.BeginInvoke(() =>
//                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
//                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
//                    );
            logger.error(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData));
            //Toaster.Show("MPNS", "Error occured: " + e.Message);

        }

        /// <summary>
        /// Event handler for when a toast notification arrives while your application is running.  
        /// The toast will not display if your application is running so you must add this
        /// event handler if you want to do something with the toast notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e) {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys) {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0) {
                    relativeUri = e.Collection[key];
                }
            }

            logger.debug("MPNS message {0}", message.ToString());
            Deployment.Current.Dispatcher.BeginInvoke(() => Toaster.Show("MPNS", message.ToString()));

//            Toaster.Show("MPNS", message.ToString());
            // Display a dialog of all the fields in the toast.
//            Dispatcher.BeginInvoke(() => MessageBox.Show(message.ToString()));

        }

    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Logging;
using Telegram.Annotations;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Logger = Telegram.Core.Logging.Logger;

namespace Telegram.Model.Wrappers {
    public delegate void ChatModelChangeHandler();
    public class ChatModel : INotifyPropertyChanged {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(ChatModel));

        private Chat chat;
        public event ChatModelChangeHandler ChangeEvent;
        public ChatModel(Chat chat) {
            this.chat = chat;
        }

        public void SetChat(Chat chat) {
            this.chat = chat;
            ChangeEvent();
            OnPropertyChanged("Title");
            OnPropertyChanged("Status");
            OnPropertyChanged("AvatarPath");
        }

        public int Id {
            get {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        return ((ChatEmptyConstructor) chat).id;
                    case Constructor.chat:
                        return ((ChatConstructor) chat).id;
                    case Constructor.chatForbidden:
                        return ((ChatForbiddenConstructor) chat).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public Chat RawChat {
            get {
                return chat;
            }
        }

        private static string[] chatPlaceholders = new string[] {
            "/Assets/UI/placeholder.group.blue-WVGA.png",
            "/Assets/UI/placeholder.group.cyan-WVGA.png",
            "/Assets/UI/placeholder.group.green-WVGA.png",
            "/Assets/UI/placeholder.group.orange-WVGA.png",
            "/Assets/UI/placeholder.group.pink-WVGA.png",
            "/Assets/UI/placeholder.group.purple-WVGA.png",
            "/Assets/UI/placeholder.group.red-WVGA.png",
            "/Assets/UI/placeholder.group.yellow-WVGA.png",
        };

        private Uri GetChatPlaceholderImageUri() {
            return new Uri(chatPlaceholders[Id % chatPlaceholders.Length], UriKind.Relative);
        }

        public BitmapImage AvatarPath {
            get {
                if (_avatarPath != null) { 
                    logger.debug("Returning cached avatar {0}", _avatarPath);
                    return Utils.Helpers.GetBitmapImageInternal(_avatarPath);
                }

                ChatPhoto avatarPhoto;
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        avatarPhoto = TL.chatPhotoEmpty();
                        break;
                    case Constructor.chat:
                        avatarPhoto = ((ChatConstructor)chat).photo;
                        break;
                    case Constructor.chatForbidden:
                        avatarPhoto = TL.chatPhotoEmpty();
                        break;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }

                FileLocation avatarFileLocation = null;

                if (avatarPhoto.Constructor != Constructor.chatPhoto) {
                    return new BitmapImage(GetChatPlaceholderImageUri());
                }

                avatarFileLocation = ((ChatPhotoConstructor)avatarPhoto).photo_small;

                Task<string> getFileTask = TelegramSession.Instance.Files.GetAvatar(avatarFileLocation);
                if (getFileTask.IsCompleted) {
                    return Utils.Helpers.GetBitmapImageInternal(getFileTask.Result);
                }

                logger.debug("File receive in progress {0}", avatarFileLocation);
                getFileTask.ContinueWith((path) => SetAvatarPath(path.Result), TaskScheduler.FromCurrentSynchronizationContext());

                return new BitmapImage(GetChatPlaceholderImageUri()); ;
            }
        }

        private string _avatarPath = null;

        public void SetAvatarPath(string path) {
            _avatarPath = path;
            logger.debug("Path saved {0}", _avatarPath);
            OnPropertyChanged("AvatarPath");
        }

        public string Title {
            get {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        return "empty";
                    case Constructor.chat:
                        return ((ChatConstructor)chat).title;
                    case Constructor.chatForbidden:
                        return ((ChatForbiddenConstructor)chat).title;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public string Status {
            get {
                switch (chat.Constructor) {
                    case Constructor.chatEmpty:
                        return "chat is empty: 0 users";
                    case Constructor.chat:
                        return ((ChatConstructor)chat).participants_count + " users";
                    case Constructor.chatForbidden:
                        return "blocked";
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            logger.debug("Invoking on propery changed for {0}, handler == null is {1}", chat, handler == null);
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        
        }
    }
}

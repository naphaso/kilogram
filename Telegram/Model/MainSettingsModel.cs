using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Annotations;
using Telegram.Model.Wrappers;
using Telegram.MTProto;

namespace Telegram.Model {
    public class MainSettingsModel : INotifyPropertyChanged {
        public ObservableCollection<MainSettingsItem> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private int selfId = TelegramSession.Instance.SelfId;

        public UserModel SelfUser {
            get {
                return TelegramSession.Instance.GetUser(selfId);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Init() {
            Items = new ObservableCollection<MainSettingsItem> {
                new MainSettingsItem() {Header = "message, group, in-app notifications", Name = "notifications"},
                new MainSettingsItem() {Header = "0 users", Name = "blocked users"},
                new MainSettingsItem() {Header = "ask a question", Name = "support"}
            };
        }

        public bool SavePhoto {
            get {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("SavePhoto")) {
                    return (bool)IsolatedStorageSettings.ApplicationSettings["SavePhoto"];
                }

                return true; 
            }
            set {
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                // txtInput is a TextBox defined in XAML.
                if (!settings.Contains("SavePhoto")) {
                    settings.Add("SavePhoto", value);
                } else {
                    settings["SavePhoto"] = value;
                }

                settings.Save();
                OnPropertyChanged();
            }
        }
    }
}

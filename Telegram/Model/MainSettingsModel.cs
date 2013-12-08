using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Annotations;

namespace Telegram.Model {
    public class MainSettingsModel : INotifyPropertyChanged {
        public ObservableCollection<MainSettingsItem> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

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
    }
}

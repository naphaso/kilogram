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
    public class DialogMessageModel {
        public ObservableCollection<DialogMessageItem> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Init() {
            Items = new ObservableCollection<DialogMessageItem> {
                new DialogMessageItem() {Text = "hello dude, whatsup!", Time = "11:01p", Sender = "Some Dude", IsOut = false},
                new DialogMessageItem() {Text = "hey...", Sender = "Some Dude", Time = "11:11p", IsOut = false},
                new DialogMessageItem() {Text = "I'll send you a lot of message, group, in-app notifications. Lorem bla bla bla. Ipsum! Dolor!", Sender = "Some Dude", IsOut = false, Time = "0:23a"},
            };
        }
    }
}

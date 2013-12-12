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
                new DialogMessageItem() {Text = "I'll send you a lot of message, group, in-app notifications. Lorem bla bla bla. Ipsum! Dolor!", Sender = "Some Dude", IsOut = true, Time = "0:23a"},
                new DialogMessageItem() {Text = "OK", Sender = "Some Dude", IsOut = false, Time = "0:24a"},
                new DialogMessageItem() {Text = "Hello 11 4242", Sender = "Another Dude", ForwardedFrom="Dude", IsOut = false, Time = "0:24a"},
                new DialogMessageItem() {Text = "asdasd asd", Sender = "Some Dude", IsOut = false, Time = "0:25a"},
                new DialogMessageItem() {Text = "eh?????", Sender = "Some Dude", IsOut = true, Time = "0:26a"},
                new DialogMessageItem() {Text = "Ok I'll do it", Sender = "Some Dude", IsOut = true, Time = "0:26a"},
                new DialogMessageItem() {Text = "wtf!", Sender = "Some Dude", IsOut = false, Time = "0:26a"},
                new DialogMessageItem() {Text = "I'm dead", Sender = "Some Dude", IsOut = false, Time = "0:26a"},

            };
        }
    }
}

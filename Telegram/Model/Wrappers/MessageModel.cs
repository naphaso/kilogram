using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Media;
using Telegram.Annotations;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public abstract class MessageModel : INotifyPropertyChanged {
        public enum MessageDeliveryState {
            Pending = 0,
            Delivered = 1,
            Read = 2,
            NoImage = 3
        }

        public class MessageAttachment {
            public bool IsLoaded { get; set; }
            public string Placeholder { get; set; }
            public BitmapImage LoadedImage { get; set; }
        }

        public abstract BitmapImage Attachment { get; }

        public abstract bool IsOut { get; }
        public abstract bool Delivered { get; } 

        public abstract string Text { get; set;  }

        public abstract DateTime Timestamp { get; set; }

        public abstract void Write(BinaryWriter writer);

        public abstract MessageDeliveryState GetMessageDeliveryState();

        public abstract string TimeString { get; }

        public abstract MessageDeliveryState MessageDeliveryStateProperty { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

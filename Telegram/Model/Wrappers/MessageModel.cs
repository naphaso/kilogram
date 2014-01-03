using System;
using System.IO;
using System.Windows;
using Microsoft.Phone.Media;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public abstract class MessageModel {
        public abstract bool Delivered { get; } 
        public abstract string Text { get; set;  }

        public abstract DateTime Timestamp { get; set; }

        public abstract void Write(BinaryWriter writer);
       
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Model;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Models {
    public class MessageBubbleTemplateSelector : DataTemplateSelector {
        public DataTemplate TextInBubbleTemplate { get; set; }
        public DataTemplate TextOutBubbleTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
//            if (item.GetType() != typeof(MessageModelUndelivered))
//                return TextInBubbleTemplate;
//            if (item.GetType() != typeof(MessageModelDelivered))
//                return TextInBubbleTemplate;

            var model = (MessageModel)item;
            return model.IsOut ? TextOutBubbleTemplate : TextInBubbleTemplate;
        }
    }
}

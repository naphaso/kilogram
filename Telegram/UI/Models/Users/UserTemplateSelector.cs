using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Telegram.UI.Models.Users {


    public class UserTemplateSelector: DataTemplateSelector {
        public DataTemplate ContactTemplate { get; set; }
        public DataTemplate FriendOnlineTemplate { get; set; }
        public DataTemplate FriendOfflineTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return FriendOnlineTemplate;
        }
    }
}

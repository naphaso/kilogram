using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Models.Users {


    public class UserTemplateSelector: DataTemplateSelector {
        public DataTemplate ContactTemplate { get; set; }
        public DataTemplate FriendOnlineTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            UserModel user = (UserModel) item;

            return FriendOnlineTemplate;
        }
    }
}

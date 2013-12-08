using System.Windows;
using System.Windows.Controls;

namespace Telegram.UI.Models.Dialogs {


    public class DialogTemplateSelector : DataTemplateSelector {
        public DataTemplate DialogTemplate { get; set; }
        public DataTemplate ChatTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return ChatTemplate;
        }
    }
}

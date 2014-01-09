using System.Windows;
using System.Windows.Controls;
using Telegram.Model.Wrappers;

namespace Telegram.UI.Models.Dialogs {


    public class DialogTemplateSelector : DataTemplateSelector {
        public DataTemplate DialogTemplate { get; set; }
        public DataTemplate ChatTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var model = item as DialogModel;
            if (model == null)
                return ChatTemplate;

            if (model.IsSecret)
                return DialogTemplate;

            return model.IsChat ? ChatTemplate : DialogTemplate;
        }
    }
}

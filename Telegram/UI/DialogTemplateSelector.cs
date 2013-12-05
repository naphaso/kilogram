using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Telegram.UI {
    public abstract class DataTemplateSelector : ContentControl {
        public virtual DataTemplate SelectTemplate(object item, DependencyObject container) {
            return null;
        }

        protected override void OnContentChanged(object oldContent, object newContent) {
            base.OnContentChanged(oldContent, newContent);

            ContentTemplate = SelectTemplate(newContent, this);
        }
    }

    public class DialogTemplateSelector : DataTemplateSelector {
        public DataTemplate DialogTemplate { get; set; }
        public DataTemplate ChatTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return ChatTemplate;
        }
    }
}

using System.Windows;
using Telegram.UI.Models;

namespace Telegram.Model {
    public class ThumbTemplateSelector : DataTemplateSelector {
        public DataTemplate VideoThumbTemplate { get; set; }
        public DataTemplate PhotoThumbTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item.GetType() != typeof(GalleryItemModel)) return PhotoThumbTemplate;

            var galleryItem = (GalleryItemModel)item;
            return galleryItem.IsVideo ? VideoThumbTemplate : PhotoThumbTemplate;
        }
    }
}

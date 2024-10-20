using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.DataTemplateSelectors {
    public class ImageDataTemplateSelector : DataTemplateSelector {
        public DataTemplate PlayableGalleryImageTemplate { get; set; }
        public DataTemplate GalleryImageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) {
            if ((item as ImageInfo).IsPlayable) {
                return PlayableGalleryImageTemplate;
            }
            return GalleryImageTemplate;
        }
    }
}

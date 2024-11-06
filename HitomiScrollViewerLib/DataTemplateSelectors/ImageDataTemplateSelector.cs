using HitomiScrollViewerLib.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.DataTemplateSelectors {
    public partial class ImageDataTemplateSelector : DataTemplateSelector {
        public DataTemplate PlayableGalleryImageTemplate { get; set; }
        public DataTemplate GalleryImageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) {
            if ((item as SizeAdjustedImageInfo).IsPlayable) {
                return PlayableGalleryImageTemplate;
            }
            return GalleryImageTemplate;
        }
    }
}

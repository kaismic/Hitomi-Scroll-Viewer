using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class GalleryLanguageASBWrapper : StackPanel {
        public GalleryLanguageASBWrapperVM ViewModel { get; set; }

        public GalleryLanguageASBWrapper() {
            InitializeComponent();
        }
    }
}

using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class GalleryLanguageASBWrapper : StackPanel {
        public GalleryLanguageASBWrapperVM ViewModel { get; set; }

        public GalleryLanguageASBWrapper() {
            InitializeComponent();
        }
    }
}

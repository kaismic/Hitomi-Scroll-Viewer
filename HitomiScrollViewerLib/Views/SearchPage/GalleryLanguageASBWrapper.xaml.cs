using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class GalleryLanguageASBWrapper : StackPanel {
        public GalleryLanguageASBWrapperVM ViewModel { get; set; }

        public GalleryLanguageASBWrapper() {
            InitializeComponent();
        }
    }
}

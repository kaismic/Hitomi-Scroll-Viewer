using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class GalleryItem : UserControl {
        private SolidColorBrush TitleBackgroundBrush { get; set; }
        private SolidColorBrush SubtitleBackgroundBrush { get; set; }
        private SolidColorBrush TextForegroundBrush { get; set; }

        private GalleryItemVM _viewModel;
        public GalleryItemVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                string typeBrush = value.Gallery.GalleryType.ToString() + "Brush";
                TitleBackgroundBrush = Resources[typeBrush + Resources["TitleBackgroundBrushNumber"]] as SolidColorBrush;
                SubtitleBackgroundBrush = Resources[typeBrush + Resources["SubtitleBackgroundBrushNumber"]] as SolidColorBrush;
                TextForegroundBrush = Resources[typeBrush + Resources["TextForegroundBrushNumber"]] as SolidColorBrush;

                
            }
        }

        public GalleryItem() {
            InitializeComponent();
            for (int i = 0; i < RootGrid.Children.Count; i++) {
                Grid.SetRow(RootGrid.Children[i] as FrameworkElement, i);
            }
        }
    }
}

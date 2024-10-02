using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Windows.UI;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class GalleryBrowseItem : UserControl {
        private SolidColorBrush TitleBackgroundBrush { get; set; }
        private SolidColorBrush SubtitleBackgroundBrush { get; set; }
        private SolidColorBrush TextForegroundBrush { get; set; }

        private GalleryBrowseItemVM _viewModel;
        public GalleryBrowseItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel != null) {
                    return;
                }
                _viewModel = value;
                string baseColorKey = value.Gallery.GalleryType.GalleryType.ToString() + "Color";
                string[] colorKeys = Enumerable.Range(0, 3).Select(i => baseColorKey + i).ToArray();
                bool isLightTheme = RequestedTheme == ElementTheme.Light;
                TitleBackgroundBrush = new((Color)Resources[colorKeys[1]]);
                SubtitleBackgroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 2 : 0]]);
                TextForegroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 0 : 2]]);
            }
        }

        public GalleryBrowseItem() {
            InitializeComponent();

            for (int i = 0; i < MainGrid.Children.Count; i++) {
                MainGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
                Grid.SetRow(MainGrid.Children[i] as FrameworkElement, i);
            }
            for (int i = 0; i < SubtitleGrid.Children.Count; i++) {
                SubtitleGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                Grid.SetColumn(SubtitleGrid.Children[i] as FrameworkElement, i);
                if (SubtitleGrid.Children[i] is TextBlock tb) {
                    tb.IsTextSelectionEnabled = true;
                }
            }
        }
    }
}

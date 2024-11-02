using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Linq;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class GalleryTabViewItem : TabViewItem {
        private GalleryTabViewItemVM _viewModel;
        public GalleryTabViewItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel == null) {
                    _viewModel = value;
                    ViewModel.RequestShowActionIcon += ShowActionIcon;
                }
            }
        }
        public GalleryTabViewItem() {
            InitializeComponent();

            TopCommandBar.PointerEntered += (_, _) => {
                TopCommandBar.Opacity = 1;
                TopCommandBar.IsOpen = true;
            };
            TopCommandBar.Closing += (_, _) => TopCommandBar.Opacity = 0;
            foreach (var control in TopCommandBar.PrimaryCommands.Cast<Control>()) {
                control.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        private void FlipView_SizeChanged(object _0, SizeChangedEventArgs e) {
            ViewModel.CurrentTabViewSize = e.NewSize;
        }

        private void FlipView_Loaded(object sender, RoutedEventArgs e) {
            FlipView flipView = sender as FlipView;
            flipView.Loaded -= FlipView_Loaded;
            // remove _flipview navigation buttons
            Grid flipViewGrid = VisualTreeHelper.GetChild(flipView, 0) as Grid;
            var children = flipViewGrid.Children;
            for (int i = children.Count - 1; i >= 0; i--) {
                if (children[i] is Button) {
                    children.RemoveAt(i);
                }
            }
        }

        private void ShowActionIcon(string glyph1, string glyph2) {
            if (glyph1 == null) {
                ActionIcon1.Opacity = 0;
            } else {
                ActionIcon1.Opacity = 1;
                ActionIcon1.Glyph = glyph1;
            }
            if (glyph2 == null) {
                ActionIcon2.Opacity = 0;
            } else {
                ActionIcon2.Opacity = 1;
                ActionIcon2.Glyph = glyph2;
            }
            FadeOutStoryboard.Begin();
        }
    }
}

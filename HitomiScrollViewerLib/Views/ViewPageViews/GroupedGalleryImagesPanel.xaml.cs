using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.ViewPageViews {
    public sealed partial class GroupedGalleryImagesPanel : Grid {
        private GroupedGalleryImagesVM _viewModel;
        public GroupedGalleryImagesVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                value.UpdateImagesRequested += SetImages;
            }
        }
        public GroupedGalleryImagesPanel() {
            InitializeComponent();
        }

        private void SetImages() {
            Children.Clear();
            // TODO
        }
    }
}
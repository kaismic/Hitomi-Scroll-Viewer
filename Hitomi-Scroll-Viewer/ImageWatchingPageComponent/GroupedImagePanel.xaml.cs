using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.ImageWatchingPageComponent {
    public sealed partial class GroupedImagePanel : DockPanel {
        private readonly List<Image> _images = [];
        private readonly List<ImageInfo> _imageInfos = [];
        private readonly Range _range;
        private readonly string _imageDir;
        public GroupedImagePanel(ViewDirection viewDirection, Range range, Gallery gallery) {
            InitializeComponent();
            _range = range;
            _imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            for (int i = range.Start.Value; i < range.End.Value; i++) {
                string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
                Image image = new() {
                    Source = files.Length > 0 ? new BitmapImage(new(files[0])) : null
                };
                _images.Add(image);
                _imageInfos.Add(gallery.files[i]);
            }
            foreach (var image in _images) {
                Children.Add(image);
            }
            UpdateViewDirection(viewDirection);
        }

        public void UpdateViewDirection(ViewDirection viewDirection) {
            foreach (var image in _images) {
                SetDock(image, viewDirection == ViewDirection.LeftToRight ? Dock.Left : Dock.Right);
            }
        }

        public void SetImageSizes(Size viewportSize) {
            double maxImgWidth = viewportSize.Width / _images.Count;
            double maxImgHeight = viewportSize.Height;
            double idealAspectRatio = maxImgWidth / maxImgHeight;
            for (int i = 0; i < _images.Count; i++) {
                double imgAspectRatio = (double)_imageInfos[i].width / _imageInfos[i].height;
                Image img = _images[i];
                BitmapImage imgSource = img.Source as BitmapImage;
                img.Width = imgAspectRatio >= idealAspectRatio ? maxImgWidth : maxImgHeight * imgAspectRatio;
                img.Height = imgAspectRatio < idealAspectRatio ? maxImgHeight : maxImgWidth / imgAspectRatio;
                if (img.Source != null) {
                    imgSource.DecodePixelWidth = (int)img.Width;
                }
            }
        }

        public void ResetImageSizes() {
            foreach (var image in _images) {
                image.Width = double.NaN;
                image.Height = double.NaN;
                if (image.Source != null) {
                    (image.Source as BitmapImage).DecodePixelWidth = 0;
                    (image.Source as BitmapImage).DecodePixelHeight = 0;
                }
            }
        }

        public void RefreshImages() {
            for (int i = 0; i < _images.Count; i++) {
                if (_images[i].Source == null) {
                    string[] files = Directory.GetFiles(_imageDir, (_range.Start.Value + i).ToString() + ".*");
                    if (files.Length > 0) {
                        _images[i].Source = new BitmapImage(new(files[0]));
                    }
                }
            }
        }
    }
}

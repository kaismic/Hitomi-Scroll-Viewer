using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;

namespace Hitomi_Scroll_Viewer.ImageWatchingPageComponent {
    public sealed partial class GroupedImagePanel : DockPanel {
        private readonly IEnumerable<Image> _images;
        private readonly IEnumerable<ImageInfo> _imageInfos;
        public GroupedImagePanel(ViewDirection viewDirection, IEnumerable<Image> images, IEnumerable<ImageInfo> imageInfos) {
            _images = images;
            _imageInfos = imageInfos;
            InitializeComponent();
            foreach (var image in images) {
                Children.Add(image);
            }
            UpdateViewDirection(viewDirection);
        }

        public void UpdateViewDirection(ViewDirection viewDirection) {
            var dock = viewDirection switch {
                ViewDirection.TopToBottom => Dock.Top,
                ViewDirection.LeftToRight => Dock.Left,
                ViewDirection.RightToLeft => Dock.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(viewDirection), $"Unexpected {typeof(ViewDirection)} value: {viewDirection}")
            };
            foreach (var image in _images) {
                SetDock(image, dock);
            }
        }

        public void SetImageSizes(ViewDirection viewDirection, (double width, double height) viewportSize) {
            int numOfPages = _images.Count();
            double maxImgWidth = viewDirection == ViewDirection.TopToBottom ? viewportSize.width : viewportSize.width / numOfPages;
            double maxImgHeight = viewDirection != ViewDirection.TopToBottom ? viewportSize.height : viewportSize.height / numOfPages;
            double idealAspectRatio = maxImgWidth / maxImgHeight;
            for (int i = 0; i < _images.Count(); i++) {
                ImageInfo imgInfo = _imageInfos.ElementAt(i);
                double imgAspectRatio = (double)imgInfo.width / imgInfo.height;
                Image img = _images.ElementAt(i);
                BitmapImage imgSource = img.Source as BitmapImage;

                img.Width = imgAspectRatio > idealAspectRatio ? maxImgWidth : maxImgHeight * imgAspectRatio;
                img.Height = imgAspectRatio <= idealAspectRatio ? maxImgHeight : maxImgWidth / imgAspectRatio;
                if (img.Source != null) {
                    imgSource.DecodePixelWidth = (int)img.Width;
                }
            }
        }
    }
}

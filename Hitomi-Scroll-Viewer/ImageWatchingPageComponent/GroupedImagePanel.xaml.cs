using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics;
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
            foreach (var image in Children) {
                SetDock(image as Image, dock);
            }
        }

        public void SetImageSizes(ViewDirection viewDirection, SizeInt32 windowSize, double rasterizationScale) {
            double dimension = viewDirection == ViewDirection.TopToBottom ? windowSize.Height : windowSize.Width - 64; // for FlipView left/right navigation button margin
            dimension = dimension / _images.Count() / rasterizationScale;
            if (viewDirection == ViewDirection.TopToBottom) {
                for (int i = 0; i < _images.Count(); i++) {
                    Image image = _images.ElementAt(i);
                    image.Width = dimension * _imageInfos.ElementAt(i).width / _imageInfos.ElementAt(i).height;
                    image.Height = dimension;
                    if (image.Source != null) {
                        (image.Source as BitmapImage).DecodePixelHeight = (int)dimension;
                    }
                }
            } else {
                for (int i = 0; i < _images.Count(); i++) {
                    Image image = _images.ElementAt(i);
                    image.Width = dimension;
                    image.Height = dimension * _imageInfos.ElementAt(i).height / _imageInfos.ElementAt(i).width;
                    if (image.Source != null) {
                        (image.Source as BitmapImage).DecodePixelWidth = (int)dimension;
                    }
                }
            }
        }
    }
}

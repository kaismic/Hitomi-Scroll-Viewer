using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Graphics;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;

namespace Hitomi_Scroll_Viewer.ImageWatchingPageComponent {
    public sealed partial class GroupedImagePanel : DockPanel {
        private readonly Image[] _images;
        public GroupedImagePanel(Image[] images, ViewDirection viewDirection) {
            _images = images;
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
                _ => throw new ArgumentOutOfRangeException(nameof(viewDirection), $"Unexpected {typeof(ViewDirection)} value: {viewDirection}"),
            };
            foreach (var image in Children) {
                SetDock(image as Image, dock);
            }
        }

        public void SetImageSizes(ViewDirection viewDirection, SizeInt32 windowSize, double rasterizationScale) {
            double dimension = viewDirection == ViewDirection.TopToBottom ? windowSize.Height : windowSize.Width;
            dimension = dimension / _images.Length / rasterizationScale;
            if (viewDirection == ViewDirection.TopToBottom) {
                foreach (var image in _images) {
                    image.Height = dimension;
                    if (image.Source != null) {
                        (image.Source as BitmapImage).DecodePixelHeight = (int)dimension;
                    }
                }
            } else {
                foreach (var image in _images) {
                    image.Width = dimension;
                    if (image.Source != null) {
                        (image.Source as BitmapImage).DecodePixelWidth = (int)dimension;
                    }
                }
            }
        }
    }
}

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
                _ => throw new ArgumentOutOfRangeException(nameof(viewDirection), $"Unexpected ViewDirection value: {viewDirection}"),
            };
            foreach (var image in Children) {
                SetDock(image as Image, dock);
            }
        }

        public void SetImageSizes(SizeInt32 windowSize, ViewDirection viewDirection) {
            bool isVertical = (viewDirection == ViewDirection.TopToBottom);
            int dimension = isVertical ? windowSize.Height / _images.Length : windowSize.Width / _images.Length;
            foreach (var image in _images) {
                if (isVertical) {
                    image.Height = dimension / _images.Length;
                    (image.Source as BitmapImage).DecodePixelHeight = dimension / _images.Length;
                } else {
                    image.Width = dimension / _images.Length;
                    (image.Source as BitmapImage).DecodePixelWidth = dimension / _images.Length;
                }
            }
        }
    }
}

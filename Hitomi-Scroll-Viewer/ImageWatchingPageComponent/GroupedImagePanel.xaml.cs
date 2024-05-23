using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;

namespace Hitomi_Scroll_Viewer.ImageWatchingPageComponent {
    public sealed partial class GroupedImagePanel : DockPanel {
        public GroupedImagePanel(Image[] images, ViewDirection viewDirection) {
            InitializeComponent();
            foreach (var image in images) {
                Children.Add(image);
            }
            UpdateLayout(viewDirection);
        }

        public void UpdateLayout(ViewDirection viewDirection) {
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
    }
}

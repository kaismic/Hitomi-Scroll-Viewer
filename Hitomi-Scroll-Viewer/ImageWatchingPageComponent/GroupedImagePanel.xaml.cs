using Microsoft.UI.Xaml.Controls;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;

namespace Hitomi_Scroll_Viewer.ImageWatchingPageComponent {
    public sealed partial class GroupedImagePanel : StackPanel {
        private readonly Image[] _images;
        public GroupedImagePanel(Image[] images, ViewDirection viewDirection) {
            _images = images;
            InitializeComponent();
            UpdateLayout(viewDirection);
        }

        public void UpdateLayout(ViewDirection viewDirection) {
            Children.Clear();
            Orientation = viewDirection == ViewDirection.TopToBottom ? Orientation.Vertical : Orientation.Horizontal;
            if (viewDirection == ViewDirection.RightToLeft) {
                foreach (var image in _images) {
                    Children.Insert(0, image);
                }
            } else {
                foreach (var image in _images) {
                    Children.Add(image);
                }
            }
        }
    }
}

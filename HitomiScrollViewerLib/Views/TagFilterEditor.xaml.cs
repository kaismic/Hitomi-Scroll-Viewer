using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class TagFilterEditor : Grid {
        public TagFilterEditorVM ViewModel { get; } = new();

        public TagFilterEditor() {
            InitializeComponent();

            for (int i = 0; i < Children.Count; i++) {
                FrameworkElement child = Children[i] as FrameworkElement;
                SetColumn(child, i);
                if (child is Button button) {
                    button.Padding = new Thickness(12);
                }
            }
        }
    }
}

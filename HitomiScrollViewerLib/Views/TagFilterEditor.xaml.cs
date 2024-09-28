using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class TagFilterEditor : Grid {
        public TagFilterEditorVM _viewModel;
        public TagFilterEditorVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                value.ShowCRUDContentDialogRequested += vm => {
                    CRUDContentDialog dialog = new() { ViewModel = vm, XamlRoot = XamlRoot };
                    return dialog.ShowAsync();
                };
            }
        }

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

using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class InputValidation : Grid {
        private InputValidationVM _viewModel;
        public InputValidationVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
                DataContext = value;
            }
        }

        public InputValidation() {
            InitializeComponent();
        }
    }
}

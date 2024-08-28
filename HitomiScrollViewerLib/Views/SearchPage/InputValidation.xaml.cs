using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class InputValidation : Grid {
        public InputValidationVM ViewModel => (InputValidationVM)DataContext;
        public InputValidation(InputValidationVM viewModel) {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

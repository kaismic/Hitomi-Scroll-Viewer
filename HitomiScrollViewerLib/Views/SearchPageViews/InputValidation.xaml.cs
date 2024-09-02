using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class InputValidation : Grid {
        public InputValidationVM ViewModel { get; set; }

        public InputValidation() {
            InitializeComponent();
        }
    }
}

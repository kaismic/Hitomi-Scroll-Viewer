using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class InputValidation : Grid {
        public InputValidationVM ViewModel { get; set; }

        public InputValidation() {
            InitializeComponent();
        }
    }
}

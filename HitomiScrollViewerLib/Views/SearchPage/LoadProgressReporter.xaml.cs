using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class LoadProgressReporter : ContentDialog {
        public LoadProgressReporterVM ViewModel { get; set; }
        public LoadProgressReporter() {
            InitializeComponent();
        }
    }
}

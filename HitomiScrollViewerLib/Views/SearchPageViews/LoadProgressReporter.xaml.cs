using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class LoadProgressReporter : ContentDialog {
        public LoadProgressReporterVM ViewModel { get; set; }
        public LoadProgressReporter() {
            InitializeComponent();
        }
    }
}

using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class LoadProgressReporter : ContentDialog {
        public LoadProgressReporterVM ViewModel { get; set; }
        public LoadProgressReporter() {
            InitializeComponent();
        }
    }
}

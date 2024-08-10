using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class MigrationProgressReporter : ContentDialog {
        public MigrationProgressReporter() {
            InitializeComponent();
            MigrationProgressBar.Value = 0;

        }

        public void SetProgressBarMaximum(int value) {
            MigrationProgressBar.Maximum = value;
        }

        public void IncrementProgressBar() {
            lock (MigrationProgressBar) {
                MigrationProgressBar.Value++;
            }
        }

        public void SetStatusMessage(string message) {
            ProgressStatusTextBlock.Text = message;
        }
    }
}

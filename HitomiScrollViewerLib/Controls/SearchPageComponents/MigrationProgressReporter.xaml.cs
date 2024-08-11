using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class MigrationProgressReporter : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(MigrationProgressReporter).Name);
        public MigrationProgressReporter() {
            InitializeComponent();
        }

        public enum DataType {
            TagFilterSets, BookmarkGalleries
        }

        public void ResetProgressBarValue() {
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

        public void SetStatusMessage(DataType dataType) {
            switch (dataType) {
                case DataType.TagFilterSets:
                    ProgressStatusTextBlock.Text = _resourceMap.GetValue("Text_Migrating_TagFilterSets").ValueAsString;
                    break;
                case DataType.BookmarkGalleries:
                    ProgressStatusTextBlock.Text = _resourceMap.GetValue("Text_Migrating_BookmarkGalleries").ValueAsString;
                    break;
            }
        }
    }
}

using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class MigrationProgressReporter : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(MigrationProgressReporter).Name);
        public MigrationProgressReporter() {
            InitializeComponent();
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

        public void SetStatusMessage(UserDataType dataType) {
            switch (dataType) {
                case UserDataType.TagFilterSet:
                    ProgressStatusTextBlock.Text = _resourceMap.GetValue("Text_Migrating_TagFilterSets").ValueAsString;
                    break;
                case UserDataType.Gallery:
                    ProgressStatusTextBlock.Text = _resourceMap.GetValue("Text_Migrating_BookmarkGalleries").ValueAsString;
                    break;
            }
        }
    }
}

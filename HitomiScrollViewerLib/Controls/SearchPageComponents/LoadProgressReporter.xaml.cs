using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class LoadProgressReporter : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(LoadProgressReporter).Name);
        public LoadProgressReporter() {
            InitializeComponent();
            Title = new TextBlock() {
                Text = _resourceMap.GetValue("Text_Title").ValueAsString,
                TextWrapping = TextWrapping.WrapWholeWords
            };
            PleaseWaitTextBlock.Text = _resourceMap.GetValue("Text_PleaseWait").ValueAsString;
        }

        public void SetProgressBarType(bool isIndeterminate) {
            MigrationProgressBar.IsIndeterminate = isIndeterminate;
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

        public enum LoadingStatus {
            LoadingDatabase,
            MigratingTFSs,
            MigratingGalleries,
            MovingImageFolder,
            AddingExampleTFSs,
            Initialising
        }

        public void SetStatusMessage(LoadingStatus loadingStatus) {
            ProgressStatusTextBlock.Text = loadingStatus switch {
                LoadingStatus.LoadingDatabase => _resourceMap.GetValue("Text_MigratingTFSs").ValueAsString,
                LoadingStatus.MigratingTFSs => _resourceMap.GetValue("Text_MigratingGalleries").ValueAsString,
                LoadingStatus.MigratingGalleries => _resourceMap.GetValue("Text_LoadingDatabase").ValueAsString,
                LoadingStatus.MovingImageFolder => _resourceMap.GetValue("Text_MovingImageFolder").ValueAsString,
                LoadingStatus.AddingExampleTFSs => _resourceMap.GetValue("Text_AddingExampleTFSs").ValueAsString,
                LoadingStatus.Initialising => _resourceMap.GetValue("Text_Initialising").ValueAsString,
                _ => throw new InvalidOperationException($"Invalid {nameof(LoadingStatus)}: {loadingStatus}"),
            };
        }
    }
}

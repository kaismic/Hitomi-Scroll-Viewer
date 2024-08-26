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

        public enum LoadingStatus {
            LoadingDatabase,
            MigratingTFSs,
            MigratingGalleries,
            MovingImageFolder,
            InitialisingDatabase,
            AddingExampleTFSs,
            InitialisingApp
        }

        public void SetStatusMessage(LoadingStatus loadingStatus) {
            ProgressStatusTextBlock.Text = loadingStatus switch {
                LoadingStatus.LoadingDatabase => _resourceMap.GetValue("Text_" + LoadingStatus.LoadingDatabase.ToString()).ValueAsString,
                LoadingStatus.MigratingTFSs => _resourceMap.GetValue("Text_" + LoadingStatus.MigratingTFSs.ToString()).ValueAsString,
                LoadingStatus.MigratingGalleries => _resourceMap.GetValue("Text_" + LoadingStatus.MigratingGalleries.ToString()).ValueAsString,
                LoadingStatus.MovingImageFolder => _resourceMap.GetValue("Text_" + LoadingStatus.MovingImageFolder.ToString()).ValueAsString,
                LoadingStatus.InitialisingDatabase => _resourceMap.GetValue("Text_" + LoadingStatus.InitialisingDatabase.ToString()).ValueAsString,
                LoadingStatus.AddingExampleTFSs => _resourceMap.GetValue("Text_" + LoadingStatus.AddingExampleTFSs.ToString()).ValueAsString,
                LoadingStatus.InitialisingApp => _resourceMap.GetValue("Text_" + LoadingStatus.InitialisingApp.ToString()).ValueAsString,
                _ => throw new InvalidOperationException($"Invalid {nameof(LoadingStatus)}: {loadingStatus}"),
            };
        }
    }
}

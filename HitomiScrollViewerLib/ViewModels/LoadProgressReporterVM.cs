using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class LoadProgressReporterVM : DQObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(LoadProgressReporter).Name);

        public string TitleText { get; } = _resourceMap.GetValue("Text_Title").ValueAsString;
        public string PleaseWaitText { get; } = _resourceMap.GetValue("Text_PleaseWait").ValueAsString;

        [ObservableProperty]
        private string _text;
        [ObservableProperty]
        private double _value;
        [ObservableProperty]
        private double _maximum;
        [ObservableProperty]
        private bool _isIndeterminate;

        public enum LoadingStatus {
            LoadingDatabase,
            MigratingTFSs,
            MigratingGalleries,
            MovingImageFolder,
            InitialisingDatabase,
            AddingExampleTFSs,
            InitialisingApp
        }

        public void SetText(LoadingStatus loadingStatus) {
            Text = loadingStatus switch {
                LoadingStatus.LoadingDatabase => _resourceMap.GetValue("Text_" + LoadingStatus.LoadingDatabase.ToString()).ValueAsString,
                LoadingStatus.MigratingTFSs => _resourceMap.GetValue("Text_" + LoadingStatus.MigratingTFSs.ToString()).ValueAsString,
                LoadingStatus.MigratingGalleries => _resourceMap.GetValue("Text_" + LoadingStatus.MigratingGalleries.ToString()).ValueAsString,
                LoadingStatus.MovingImageFolder => _resourceMap.GetValue("Text_" + LoadingStatus.MovingImageFolder.ToString()).ValueAsString,
                LoadingStatus.InitialisingDatabase => _resourceMap.GetValue("Text_" + LoadingStatus.InitialisingDatabase.ToString()).ValueAsString,
                LoadingStatus.AddingExampleTFSs => _resourceMap.GetValue("Text_" + LoadingStatus.AddingExampleTFSs.ToString()).ValueAsString,
                LoadingStatus.InitialisingApp => _resourceMap.GetValue("Text_" + LoadingStatus.InitialisingApp.ToString()).ValueAsString,
                _ => throw new ArgumentException($"Invalid {nameof(LoadingStatus)}: {loadingStatus}", nameof(loadingStatus)),
            };
        }
    }
}

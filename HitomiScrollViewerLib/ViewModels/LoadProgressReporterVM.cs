using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class LoadProgressReporterVM : DQObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(LoadProgressReporter).Name);

        public string TitleText { get; } = _resourceMap.GetValue("Title").ValueAsString;
        public string PleaseWaitText { get; } = _resourceMap.GetValue("PleaseWait").ValueAsString;

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
            RenamingImageFiles,
            InitialisingDatabase,
            AddingExampleTFSs,
            InitialisingApp
        }

        public void SetText(LoadingStatus loadingStatus) {
            Text = loadingStatus switch {
                LoadingStatus.LoadingDatabase => _resourceMap.GetValue(LoadingStatus.LoadingDatabase.ToString()).ValueAsString,
                LoadingStatus.MigratingTFSs => _resourceMap.GetValue(LoadingStatus.MigratingTFSs.ToString()).ValueAsString,
                LoadingStatus.MigratingGalleries => _resourceMap.GetValue(LoadingStatus.MigratingGalleries.ToString()).ValueAsString,
                LoadingStatus.MovingImageFolder => _resourceMap.GetValue(LoadingStatus.MovingImageFolder.ToString()).ValueAsString,
                LoadingStatus.RenamingImageFiles => _resourceMap.GetValue(LoadingStatus.RenamingImageFiles.ToString()).ValueAsString,
                LoadingStatus.InitialisingDatabase => _resourceMap.GetValue(LoadingStatus.InitialisingDatabase.ToString()).ValueAsString,
                LoadingStatus.AddingExampleTFSs => _resourceMap.GetValue(LoadingStatus.AddingExampleTFSs.ToString()).ValueAsString,
                LoadingStatus.InitialisingApp => _resourceMap.GetValue(LoadingStatus.InitialisingApp.ToString()).ValueAsString,
                _ => throw new ArgumentException($"Invalid {nameof(LoadingStatus)}: {loadingStatus}", nameof(loadingStatus)),
            };
        }
    }
}

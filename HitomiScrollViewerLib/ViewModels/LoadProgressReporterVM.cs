using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.Views;
using System;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class LoadProgressReporterVM : DQObservableObject {
        private static readonly string SUBTREE_NAME = typeof(LoadProgressReporter).Name;

        public string TitleText { get; } = "Title".GetLocalized(SUBTREE_NAME);
        public string PleaseWaitText { get; } = "PleaseWait".GetLocalized(SUBTREE_NAME);

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
            MigratingTFs,
            MigratingGalleries,
            MovingImageFolder,
            RenamingImageFiles,
            InitialisingDatabase,
            AddingExampleTFs,
            InitialisingApp
        }

        public void SetText(LoadingStatus loadingStatus) {
            Text = loadingStatus switch {
                LoadingStatus.LoadingDatabase => LoadingStatus.LoadingDatabase.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.MigratingTFs => LoadingStatus.MigratingTFs.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.MigratingGalleries => LoadingStatus.MigratingGalleries.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.MovingImageFolder => LoadingStatus.MovingImageFolder.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.RenamingImageFiles => LoadingStatus.RenamingImageFiles.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.InitialisingDatabase => LoadingStatus.InitialisingDatabase.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.AddingExampleTFs => LoadingStatus.AddingExampleTFs.ToString().GetLocalized(SUBTREE_NAME),
                LoadingStatus.InitialisingApp => LoadingStatus.InitialisingApp.ToString().GetLocalized(SUBTREE_NAME),
                _ => throw new ArgumentException($"Invalid {nameof(LoadingStatus)}: {loadingStatus}", nameof(loadingStatus)),
            };
        }
    }
}

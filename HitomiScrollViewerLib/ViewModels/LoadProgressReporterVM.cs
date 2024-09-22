using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class LoadProgressReporterVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(LoadProgressReporter).Name);

        public string TitleText { get; } = _resourceMap.GetValue("Text_Title").ValueAsString;
        public string PleaseWaitText { get; } = _resourceMap.GetValue("Text_PleaseWait").ValueAsString;

        private string _text;
        public string Text {
            get => _text;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _text, value);
                });
            }
        }
        private double _value;
        public double Value {
            get => _value;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _value, value);
                });
            }
        }
        private double _maximum;
        public double Maximum {
            get => _maximum;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _maximum, value);
                });
            }
        }
        private bool _isIndeterminate;
        public bool IsIndeterminate {
            get => _isIndeterminate;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _isIndeterminate, value);
                });
            }
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

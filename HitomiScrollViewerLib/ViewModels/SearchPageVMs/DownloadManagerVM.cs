using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class DownloadManagerVM {
        private int _currentDownloadCount = 0;
        public ObservableCollection<DownloadItemVM> DownloadItemVMs { get; } = [];
        public event Action GalleryAdded;
        public event Action<Gallery> TrySetImageSourceRequested;
        public int DownloadThreadNum { get; set; } = 1;
        private bool _isSequentialDownload = true;
        public bool IsSequentialDownload {
            get => _isSequentialDownload;
            set {
                _isSequentialDownload = value;
                if (!value) {
                    foreach (var vm in DownloadItemVMs) {
                        if (!vm.HasStarted) {
                            vm.StartDownload();
                        }
                    }
                }
            }
        }

        public void TryDownload(int id) {
            if (DownloadItemVMs.Select(d => d.Id).Contains(id)) {
                return;
            }
            DownloadItemVM vm = new(id, DownloadThreadNum);
            DownloadItemVMs.Add(vm);
            vm.InvokeGalleryAddedRequested += GalleryAdded.Invoke;
            vm.DownloadStarted += () => {
                Interlocked.Increment(ref _currentDownloadCount);
            };
            vm.RemoveDownloadItemEvent += (DownloadItemVM arg) => {
                if (arg.HasStarted) {
                    Interlocked.Decrement(ref _currentDownloadCount);
                }
                DownloadItemVMs.Remove(arg);
                TrySetImageSourceRequested?.Invoke(arg.Gallery);
                StartNextDownload();
            };
            if (IsSequentialDownload) {
                StartNextDownload();
            } else {
                vm.StartDownload();
            }
        }

        private void StartNextDownload() {
            if (IsSequentialDownload && _currentDownloadCount == 0) {
                foreach (var vm in DownloadItemVMs) {
                    if (!vm.HasStarted) {
                        vm.StartDownload();
                        break;
                    }
                }
            }
        }
    }
}

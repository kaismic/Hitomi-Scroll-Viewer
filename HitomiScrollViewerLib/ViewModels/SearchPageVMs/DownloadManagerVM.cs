using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class DownloadManagerVM {
        public ConcurrentQueue<DownloadItemVM> DownloadItemsQueue { get; } = [];
        public ObservableCollection<DownloadItemVM> DownloadItemVMs { get; } = [];
        public event Action GalleryAdded;
        public event Action<Gallery> TrySetImageSourceRequested;

        public int DownloadThreadNum { get; set; } = 1;
        public bool IsSequentialDownload { get; set; } = true;

        public void TryDownload(int id) {
            if (DownloadItemVMs.Select(d => d.Id).Contains(id)) {
                return;
            }
            DownloadItemVM vm = new(id, DownloadThreadNum);
            DownloadItemVMs.Add(vm);
            vm.RemoveDownloadItemEvent += (DownloadItemVM arg) => {
                DownloadItemVMs.Remove(arg);
                TrySetImageSourceRequested?.Invoke(arg.Gallery);
            };
            vm.InvokeGalleryAddedRequested += GalleryAdded.Invoke;
            // TODO download queue system
            if (IsSequentialDownload) {
                DownloadItemsQueue.Enqueue(vm);
            } else {
                vm.StartDownload();
            }
        }
    }
}

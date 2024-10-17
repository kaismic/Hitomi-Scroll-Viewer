using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class DownloadManagerVM {
        public ObservableCollection<DownloadItemVM> DownloadItemVMs { get; } = [];
        public event Action GalleryAdded;
        public event Action<Gallery> TrySetImageSourceRequested;

        public void TryDownload(int id) {
            if (DownloadItemVMs.Select(d => d.Id).Contains(id)) {
                return;
            }
            DownloadItemVM vm = new(id);
            DownloadItemVMs.Add(vm);
            vm.RemoveDownloadItemEvent += (DownloadItemVM arg) => {
                DownloadItemVMs.Remove(arg);
                TrySetImageSourceRequested?.Invoke(arg.Gallery);
            };
            vm.InvokeGalleryAddedRequested += GalleryAdded.Invoke;
            vm.StartDownload();
        }
    }
}

using HitomiScrollViewerLib.Views.BrowsePageViews;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class DownloadManagerVM {
        public ObservableCollection<DownloadItemVM> DownloadItemVMs { get; } = [];
        private readonly ConcurrentDictionary<int, byte> _downloadingGalleryIds = [];

        private static DownloadManagerVM _main;
        public static DownloadManagerVM Main => _main ??= new();
        private DownloadManagerVM() { }

        public bool TryDownload(int id) {
            if (_downloadingGalleryIds.TryAdd(id, 0)) {
                DownloadItemVM vm = new(id);
                DownloadItemVMs.Add(vm);
                vm.RemoveDownloadItemEvent += RemoveDownloadItem;
                vm.UpdateIdEvent += UpdateId;
                vm.StartDownload();
                return true;
            }
            return false;
        }

        private void UpdateId(int oldId, int newId) {
            _downloadingGalleryIds.Remove(oldId, out _);
            _downloadingGalleryIds.TryAdd(newId, 0);
        }

        private void RemoveDownloadItem(DownloadItemVM sender, int id) {
            _downloadingGalleryIds.Remove(id, out _);
            DownloadItemVMs.Remove(sender);
        }
    }
}

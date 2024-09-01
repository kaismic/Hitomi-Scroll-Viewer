using HitomiScrollViewerLib.Controls.SearchPageComponents;
using HitomiScrollViewerLib.Views.BrowsePage;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    internal class DownloadManagerVM {
        internal ObservableCollection<DownloadItemVM> DownloadItemVMs { get; } = [];
        private readonly ConcurrentDictionary<int, byte> _downloadingGalleryIds = [];

        public bool TryDownload(int id, BookmarkItem bookmarkItem = null) {
            if (_downloadingGalleryIds.TryAdd(id, 0)) {
                DownloadItemVM vm = new(id, bookmarkItem);
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

        public bool HasAnyDownloads() {
            return !_downloadingGalleryIds.IsEmpty;
        }
    }
}

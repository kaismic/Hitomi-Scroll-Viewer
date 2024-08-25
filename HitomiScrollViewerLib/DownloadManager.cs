using HitomiScrollViewerLib.Controls.SearchPageComponents;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib {
    internal class DownloadManager {
        internal ObservableCollection<DownloadItem> DownloadItems { get; } = [];
        private readonly ConcurrentDictionary<int, byte> _downloadingGalleryIds = [];

        internal bool TryDownload(int id, BookmarkItem bookmarkItem = null) {
            if (_downloadingGalleryIds.TryAdd(id, 0)) {
                DownloadItem downloadItem = new(id, bookmarkItem);
                DownloadItems.Add(downloadItem);
                downloadItem.RemoveDownloadItemEvent += RemoveDownloadItem;
                downloadItem.UpdateIdEvent += UpdateId;
                downloadItem.InitDownload();
                return true;
            }
            return false;
        }

        private void UpdateId(int oldId, int newId) {
            _downloadingGalleryIds.Remove(oldId, out _);
            _downloadingGalleryIds.TryAdd(newId, 0);
        }

        private void RemoveDownloadItem(DownloadItem sender, int id) {
            _downloadingGalleryIds.Remove(id, out _);
            DownloadItems.Remove(sender);
        }

        public bool HasAnyDownloads() {
            return !_downloadingGalleryIds.IsEmpty;
        }
    }
}

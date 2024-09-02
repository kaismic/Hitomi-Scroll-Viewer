using HitomiScrollViewerLib.Views;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class DownloadManagerVM : IAppWindowClosingHandler {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(DownloadManagerVM).Name);
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

        public async void HandleAppWindowClosing(AppWindowClosingEventArgs args) {
            if (!_downloadingGalleryIds.IsEmpty) {
                ContentDialog dialog = new() {
                    DefaultButton = ContentDialogButton.Close,
                    Title = _resourceMap.GetValue("Text_DownloadRemaining").ValueAsString,
                    PrimaryButtonText = TEXT_EXIT,
                    CloseButtonText = TEXT_CANCEL,
                    XamlRoot = MainWindow.CurrMW.Content.XamlRoot
                };
                if ((await dialog.ShowAsync()) == ContentDialogResult.None) {
                    args.Cancel = true;
                    return;
                }
            }
        }
    }
}

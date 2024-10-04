using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.Entities;
using System;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public class SortItemVM {
        public GallerySortEntity GallerySort { get; }
        public RelayCommand RemoveCommand { get; }
        public event Action<SortItemVM> RemoveRequested;
        public event Action<SortItemVM> AddRequested;

        public SortItemVM(GallerySortEntity gallerySort) {
            GallerySort = gallerySort;
            RemoveCommand = new RelayCommand(() => RemoveRequested.Invoke(this));
        }

        public void InvokeAddRequested() {
            AddRequested?.Invoke(this);
        }
    }
}

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public class SortItemVM {
        public GallerySortEntity GallerySort { get; }
        public SortDirectionEntity[] SortDirections { get; }
        public RelayCommand RemoveCommand { get; }
        public event Action<SortItemVM> RemoveRequested;
        public event Action<SortItemVM> AddRequested;

        public SortItemVM(GallerySortEntity gallerySort, SortDirectionEntity[] sortDirections) {
            GallerySort = gallerySort;
            SortDirections = sortDirections;
            RemoveCommand = new RelayCommand(() => RemoveRequested.Invoke(this));
        }

        public void InvokeAddRequested() {
            AddRequested?.Invoke(this);
        }

        public IOrderedEnumerable<Gallery> SortGallery(IEnumerable<Gallery> galleries) =>
            GallerySort.SortDirectionEntity.SortDirection == SortDirection.Ascending ?
                galleries.OrderBy(GetSortKey) :
                galleries.OrderByDescending(GetSortKey);

        public IOrderedEnumerable<Gallery> ThenSortGallery(IOrderedEnumerable<Gallery> galleries) =>
            GallerySort.SortDirectionEntity.SortDirection == SortDirection.Ascending ?
                galleries.ThenBy(GetSortKey) :
                galleries.ThenByDescending(GetSortKey);

        private object GetSortKey(Gallery g) {
            return GallerySort.GallerySortProperty switch {
                GallerySortProperty.Id => g.Id,
                GallerySortProperty.Title => g.Title,
                GallerySortProperty.Date => g.Date,
                GallerySortProperty.LastDownloadTime => g.LastDownloadTime,
                GallerySortProperty.GalleryType => g.GalleryType.GalleryType,
                GallerySortProperty.GalleryLanguage => g.GalleryLanguage.SearchParamValue,
                _ => throw new InvalidOperationException()
            };
        }
    }
}

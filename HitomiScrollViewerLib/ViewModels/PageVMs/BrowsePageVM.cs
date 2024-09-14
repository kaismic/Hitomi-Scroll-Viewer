using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class BrowsePageVM : ObservableObject {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();

        public int[] PageSizes { get; } = Enumerable.Range(1, 15).ToArray();

        public TagFilterEditorVM TagFilterSetEditorVM { get; set; }
        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];

        [ObservableProperty]
        private List<int> _pageIndexes;

        [ObservableProperty]
        private int _pageIndex = 0;
        partial void OnPageIndexChanged(int value) {
            CurrentGalleryItemVMs =
                FilteredGalleries
                .Skip(value * PageSize)
                .Take(PageSize)
                .Select(g => new GalleryItemVM() { Gallery = g })
                .ToList();
        }

        [ObservableProperty]
        private int _pageSize = 5;
        partial void OnPageSizeChanged(int value) {
            PageIndexes = [.. Enumerable.Range(1, (int)Math.Ceiling((double)_filteredGalleryCount / PageSize))];
        }

        private int _filteredGalleryCount = HitomiContext.Main.Galleries.Count();

        [ObservableProperty]
        private IEnumerable<Gallery> _filteredGalleries = HitomiContext.Main.Galleries;
        partial void OnFilteredGalleriesChanged(IEnumerable<Gallery> value) {
            _filteredGalleryCount = value.Count();
        }

        [ObservableProperty]
        public List<GalleryItemVM> _currentGalleryItemVMs;

        [RelayCommand(CanExecute = nameof(CanCreateGalleryFilter))]
        private void CreateGalleryFilter() {
            SearchFilterVM vm = TagFilterSetEditorVM.GetSearchFilterVM();
            if (vm != null) {
                // copy link to clipboard
                vm.DeleteCommand.Command = new RelayCommand<SearchFilterVM>(arg => {
                    SearchFilterVMs.Remove(arg);
                });
                SearchFilterVMs.Add(vm);
            }
        }
        private bool CanCreateGalleryFilter() => TagFilterSetEditorVM.AnyFilterSelected;

        public void FilterGalleries(SearchFilterVM vm) {
            FilteredGalleries = vm.GetFilteredGalleries();
        }
    }
}

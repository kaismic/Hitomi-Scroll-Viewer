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
    public partial class BrowsePageVM : DQObservableObject {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();
        private BrowsePageVM() { }

        public int[] PageSizes { get; } = Enumerable.Range(1, 15).ToArray();

        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];

        [ObservableProperty]
        private List<int> _pageIndexes;

        [ObservableProperty]
        private int _pageIndex = 0;
        partial void OnPageIndexChanged(int value) {
            CurrentGalleryBrowseItemVMs =
                FilteredGalleries
                .Skip(value * PageSize)
                .Take(PageSize)
                .Select(g => new GalleryBrowseItemVM() { Gallery = g })
                .ToList();
        }

        [ObservableProperty]
        private int _pageSize = 5;
        partial void OnPageSizeChanged(int value) {
            PageIndexes = [.. Enumerable.Range(1, (int)Math.Ceiling((double)_filteredGalleryCount / value))];
        }

        private int _filteredGalleryCount = HitomiContext.Main.Galleries.Count();

        [ObservableProperty]
        private IEnumerable<Gallery> _filteredGalleries = HitomiContext.Main.Galleries;
        partial void OnFilteredGalleriesChanged(IEnumerable<Gallery> value) {
            _filteredGalleryCount = value.Count();
            PageIndex = 0;
        }

        [ObservableProperty]
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs;

        [RelayCommand(CanExecute = nameof(CanCreateGalleryFilter))]
        private void CreateGalleryFilter() {
            SearchFilterVM vm = TagFilterEditorVM.GetSearchFilterVM();
            if (vm != null) {
                vm.DeleteCommand.Command = new RelayCommand<SearchFilterVM>(arg => SearchFilterVMs.Remove(arg));
                vm.SearchFilterClicked += arg => FilteredGalleries = arg.GetFilteredGalleries();
                SearchFilterVMs.Add(vm);
            }
        }
        private bool CanCreateGalleryFilter() => TagFilterEditorVM.AnyFilterSelected;



        public IEnumerable<Gallery> GetFilteredGalleries() {
            IEnumerable<Gallery> filtered = null;
            if (GalleryLanguage != null) {
                filtered = filtered == null ? GalleryLanguage.Galleries : filtered.Intersect(GalleryLanguage.Galleries);
            }
            if (GalleryType != null) {
                filtered = filtered == null ? GalleryType.Galleries : filtered.Intersect(GalleryType.Galleries);
            }
            if (IncludeTags.Any()) {
                filtered ??= HitomiContext.Main.Galleries;
                foreach (Tag includeTag in IncludeTags) {
                    filtered = filtered.Intersect(includeTag.Galleries);
                }
            }
            if (ExcludeTags.Any()) {
                filtered ??= HitomiContext.Main.Galleries;
                foreach (Tag excludeTag in ExcludeTags) {
                    filtered = filtered.Except(excludeTag.Galleries);
                }
            }
            if (SearchTitleText != null) {
                filtered = filtered == null ? HitomiContext.Main.Galleries : filtered.Where(g => g.Title.Contains(SearchTitleText));
            }
            return filtered;
        }
    }
}
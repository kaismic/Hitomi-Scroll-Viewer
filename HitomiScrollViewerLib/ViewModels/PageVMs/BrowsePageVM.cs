using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using HitomiScrollViewerLib.Views;
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

        private List<int> _pageIndexes;
        public List<int> PageIndexes {
            get => _pageIndexes;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _pageIndexes, value);
                });
            }
        }

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

        private int _pageSize = 5;
        public int PageSize {
            get => _pageSize;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _pageSize, value)) {
                        PageIndexes = [.. Enumerable.Range(1, (int)Math.Ceiling((double)_filteredGalleryCount / PageSize))];
                    }
                });
            }
        }

        private int _filteredGalleryCount = HitomiContext.Main.Galleries.Count();


        private IEnumerable<Gallery> _filteredGalleries = HitomiContext.Main.Galleries;
        public IEnumerable<Gallery> FilteredGalleries {
            get => _filteredGalleries;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _filteredGalleries, value)) {
                        _filteredGalleryCount = value.Count();
                    }
                });
            }
        }
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs;
        public List<GalleryBrowseItemVM> CurrentGalleryBrowseItemVMs {
            get => _currentGalleryBrowseItemVMs;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _currentGalleryBrowseItemVMs, value);
                });
            }
        }


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

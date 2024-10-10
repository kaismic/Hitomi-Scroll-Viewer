using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class BrowsePageVM : DQObservableObject, IDisposable {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();
        private readonly HitomiContext _context = new();

        public QueryBuilderVM QueryBuilderVM { get; }
        public SortDialogVM SortDialogVM { get; }

        private BrowsePageVM() {
            SortDialogVM = new(_context);
            QueryBuilderVM = new(
                _context,
                _context.QueryConfigurations.Find(PageKind.BrowsePage),
                [.. _context.GalleryLanguages],
                [.. _context.GalleryTypes]
            );
            QueryBuilderVM.InsertTags([.. QueryBuilderVM.QueryConfiguration.Tags]);
            QueryBuilderVM.TagCollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        QueryBuilderVM.QueryConfiguration.Tags.Add(e.NewItems[0] as Tag);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        QueryBuilderVM.QueryConfiguration.Tags.Remove(e.OldItems[0] as Tag);
                        break;
                }
            };
            SortDialogVM.ActiveSortItemVMs.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => ExecuteQuery();
            foreach (GallerySortEntity gs in _context.GallerySorts.Include(gs => gs.SortDirectionEntity).ToList()) {
                gs.SortDirectionChanged += ExecuteQuery;
            }
            SearchPageVM.Main.DownloadManagerVM.GalleryAdded += ExecuteQuery;
            SearchPageVM.Main.DownloadManagerVM.TrySetImageSourceRequested += (Gallery g) => {
                CurrentGalleryBrowseItemVMs.First(vm => vm.Gallery.Id == g.Id).InvokeTrySetImageSourceRequested();
            };

            IncrementCommand = new RelayCommand(Increment, CanIncrement);
            DecrementCommand = new RelayCommand(Decrement, CanDecrement);

            ExecuteQuery();
        }

        private void SetPages() {
            int newPagesCount = (int)Math.Ceiling((double)FilteredGalleries.Count / PageSizes[SelectedPageSizeIndex]);
            if (Pages == null || newPagesCount != Pages.Count) {
                Pages = [.. Enumerable.Range(1, newPagesCount)];
            }
        }

        public event Action CurrentGalleryBrowseItemsChanged;

        private void SetCurrentGalleryBrowseItemVMs() {
            CurrentGalleryBrowseItemVMs =
            [.. FilteredGalleries
                .Skip(SelectedPageIndex * PageSizes[SelectedPageSizeIndex])
                .Take(PageSizes[SelectedPageSizeIndex])
                .Select(g => new GalleryBrowseItemVM(g))
            ];
            CurrentGalleryBrowseItemsChanged?.Invoke();
            IncrementCommand.NotifyCanExecuteChanged();
            DecrementCommand.NotifyCanExecuteChanged();

        }

        public int[] PageSizes { get; } = Enumerable.Range(1, 15).ToArray();

        [ObservableProperty]
        private List<int> _pages;

        [ObservableProperty]
        private int _selectedPageIndex = -1;
        partial void OnSelectedPageIndexChanged(int value) {
            if (value == -1 || FilteredGalleries.Count == 0) {
                CurrentGalleryBrowseItemVMs = [];
                return;
            }
            SetCurrentGalleryBrowseItemVMs();
        }

        public RelayCommand IncrementCommand { get; }
        public RelayCommand DecrementCommand { get; }
        private void Increment() {
            SelectedPageIndex++;
        }
        private void Decrement() {
            SelectedPageIndex--;
        }
        private bool CanIncrement() => SelectedPageIndex >= 0 && Pages[SelectedPageIndex] * PageSizes[SelectedPageSizeIndex] < FilteredGalleries.Count;
        private bool CanDecrement() => SelectedPageIndex > 0;

        [ObservableProperty]
        private int _selectedPageSizeIndex = 3;
        partial void OnSelectedPageSizeIndexChanged(int value) {
            SetPages();
            SelectedPageIndex = 0;
        }

        [ObservableProperty]
        private List<Gallery> _filteredGalleries;
        partial void OnFilteredGalleriesChanged(List<Gallery> value) {
            SetPages();
            if (value.Count > 0) {
                if (SelectedPageIndex != 0) {
                    SelectedPageIndex = 0;
                } else {
                    SetCurrentGalleryBrowseItemVMs();
                }
            }
        }

        [ObservableProperty]
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs = [];

        [RelayCommand]
        private void ExecuteQuery() {
            using HitomiContext context = new();
            IEnumerable<Gallery> filtered = [
                .. context.Galleries
                .Include(g => g.GalleryType)
                .Include(g => g.GalleryLanguage)
                .Include(g => g.Files)
                .Include(g => g.Tags)
            ];
            if (!QueryBuilderVM.QueryConfiguration.SelectedLanguage.IsAll) {
                context.Entry(QueryBuilderVM.QueryConfiguration.SelectedLanguage).Collection(l => l.Galleries).Load();
                if (QueryBuilderVM.QueryConfiguration.SelectedLanguage.Galleries.Count == 0) {
                    FilteredGalleries = [];
                    return;
                }
                filtered = filtered.Intersect(QueryBuilderVM.QueryConfiguration.SelectedLanguage.Galleries);
            }
            if (QueryBuilderVM.QueryConfiguration.SelectedType.GalleryType != GalleryType.All) {
                context.Entry(QueryBuilderVM.QueryConfiguration.SelectedType).Collection(t => t.Galleries).Load();
                if (QueryBuilderVM.QueryConfiguration.SelectedType.Galleries.Count == 0) {
                    FilteredGalleries = [];
                    return;
                }
                filtered = filtered.Intersect(QueryBuilderVM.QueryConfiguration.SelectedType.Galleries);
            }
            HashSet<Tag> currentTags = QueryBuilderVM.GetCurrentTags();
            foreach (Tag tag in currentTags) {
                context.Entry(tag).Collection(t => t.Galleries).Load();
                filtered = filtered.Intersect(tag.Galleries);
            }
            if (QueryBuilderVM.SearchTitleText.Length > 0) {
                filtered = filtered.Where(g => g.Title.Contains(QueryBuilderVM.SearchTitleText));
            }
            // sort
            if (SortDialogVM.ActiveSortItemVMs.Count > 0) {
                filtered = SortDialogVM.ActiveSortItemVMs[0].SortGallery(filtered);
                for (int i = 1; i < SortDialogVM.ActiveSortItemVMs.Count; i++) {
                    filtered = SortDialogVM.ActiveSortItemVMs[i].ThenSortGallery(filtered as IOrderedEnumerable<Gallery>);
                }
            }
            FilteredGalleries = [.. filtered];
        }

        public void Dispose() {
            _context.SaveChanges();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
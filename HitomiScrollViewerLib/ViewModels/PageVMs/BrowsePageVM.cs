using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class BrowsePageVM : DQObservableObject, IDisposable {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();
        private readonly HitomiContext _context = new();

        private BrowsePageVM() {
            SortDialogVM = new(_context);
            QueryBuilderVM = new(_context, "BrowsePageGalleryLanguageIndex", "BrowsePageGalleryTypeIndex");
            QueryBuilderVM.InsertTags([.. _context.Tags.Where(t => t.IsSavedBrowseTag)]);
            QueryBuilderVM.TagCollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        (e.NewItems[0] as Tag).IsSavedBrowseTag = true;
                        _context.SaveChanges();
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        (e.OldItems[0] as Tag).IsSavedBrowseTag = false;
                        _context.SaveChanges();
                        break;
                }
            };
            SortDialogVM.ActiveSortItemVMs.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => ExecuteQuery();
            foreach (GallerySortEntity gs in _context.GallerySorts.Include(gs => gs.SortDirectionEntity).ToList()) {
                gs.SortDirectionChanged += ExecuteQuery;
            }
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

        [ObservableProperty]
        private int _selectedPageSizeIndex = 4;
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
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs;

        public QueryBuilderVM QueryBuilderVM { get; }
        public SortDialogVM SortDialogVM { get; }

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
            if (QueryBuilderVM.GalleryLanguageSelectedIndex > 0) {
                if (QueryBuilderVM.SelectedGalleryLanguage.Galleries == null) {
                    FilteredGalleries = [];
                    return;
                }
                filtered = filtered.Intersect(QueryBuilderVM.SelectedGalleryLanguage.Galleries);
            }
            if (QueryBuilderVM.GalleryTypeSelectedIndex > 0) {
                if (QueryBuilderVM.SelectedGalleryTypeEntity.Galleries == null) {
                    FilteredGalleries = [];
                    return;
                }
                filtered = filtered?.Intersect(QueryBuilderVM.SelectedGalleryTypeEntity.Galleries);
            }
            HashSet<Tag> currentTags = QueryBuilderVM.GetCurrentTags();
            if (currentTags.Count > 0) {
                foreach (Tag tag in currentTags) {
                    context.Entry(tag).Collection(t => t.Galleries).Load();
                    if (tag.Galleries is not null) {
                        filtered = filtered.Intersect(tag.Galleries);
                    }
                }
            }
            if (QueryBuilderVM.SearchTitleText.Length != 0) {
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
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
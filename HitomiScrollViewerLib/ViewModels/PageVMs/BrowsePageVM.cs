using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Storage;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class BrowsePageVM : DQObservableObject {
        public static BrowsePageVM Main { get; private set; }
        public QueryBuilderVM QueryBuilderVM { get; }
        public SortDialogVM SortDialogVM { get; }

        public event Action<IEnumerable<Gallery>> OpenGalleriesRequested;
        public event Action NavigateToViewPageRequested;
        public event Action<Gallery> FocusGalleryTabViewItemRequested;

        private BrowsePageVM() {
            SortDialogVM = new();
            QueryBuilderVM = new(PageKind.BrowsePage);
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
            SortDialogVM.SortDirectionChanged += ExecuteQuery;
            SearchPageVM.Main.DownloadManagerVM.GalleryAdded += ExecuteQuery;
            SearchPageVM.Main.DownloadManagerVM.TrySetImageSourceRequested += (Gallery g) => {
                if (CurrentGalleryBrowseItemVMs.Select(vm => vm.Gallery.Id).Contains(g.Id)) {
                    CurrentGalleryBrowseItemVMs.First(vm => vm.Gallery.Id == g.Id).InvokeTrySetImageSourceRequested();
                }
            };

            IncrementCommand = new RelayCommand(Increment, CanIncrement);
            DecrementCommand = new RelayCommand(Decrement, CanDecrement);

            ExecuteQuery();
        }

        public static void Init() {
            Main = new();
        }

        private void SetPages() {
            Pages = [.. Enumerable.Range(1, (int)Math.Ceiling((double)FilteredGalleries.Count / PageSizes[SelectedPageSizeIndex]))];
            if (Pages.Count > 0) {
                SelectedPageIndex = 0;
            }
        }

        public event Action CurrentGalleryBrowseItemsChanged;

        private async void SetCurrentGalleryBrowseItemVMs() {
            List<GalleryBrowseItemVM> temp = [];
            if (FilteredGalleries.Count > 0) {
                IEnumerable<Gallery> galleries =
                    FilteredGalleries.Skip(SelectedPageIndex * PageSizes[SelectedPageSizeIndex])
                    .Take(PageSizes[SelectedPageSizeIndex]);
                foreach (Gallery gallery in galleries) {
                    GalleryBrowseItemVM vm = new(gallery);
                    await vm.Init();
                    vm.OpenCommand.ExecuteRequested += (_, _) => {
                        if (SelectedGalleryBrowseItemVMs != null) {
                            var galleries = SelectedGalleryBrowseItemVMs.Select(selectedVM => selectedVM.Gallery);
                            OpenGalleriesRequested.Invoke(galleries);
                            NavigateToViewPageRequested.Invoke();
                            FocusGalleryTabViewItemRequested.Invoke(SelectedGalleryBrowseItemVMs[0].Gallery);
                        }
                    };
                    vm.DeleteCommand.ExecuteRequested += (_, _) => {
                        if (SelectedGalleryBrowseItemVMs != null) {
                            GalleryDAO.RemoveRange(SelectedGalleryBrowseItemVMs.Select(vm => vm.Gallery));
                            ExecuteQuery();
                        }
                    };
                    temp.Add(vm);
                }
            }
            CurrentGalleryBrowseItemVMs = temp;
            CurrentGalleryBrowseItemsChanged?.Invoke();
            IncrementCommand.NotifyCanExecuteChanged();
            DecrementCommand.NotifyCanExecuteChanged();
        }

        public List<GalleryBrowseItemVM> SelectedGalleryBrowseItemVMs { get; set; }

        public int[] PageSizes { get; } = Enumerable.Range(1, 15).ToArray();

        [ObservableProperty]
        private List<int> _pages;

        [ObservableProperty]
        private int _selectedPageIndex = -1;
        partial void OnSelectedPageIndexChanged(int value) {
            if (value == -1 && FilteredGalleries.Count > 0) {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
                _selectedPageIndex = 0;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
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


        private int _selectedPageSizeIndex = (int)(ApplicationData.Current.LocalSettings.Values[nameof(SelectedPageSizeIndex)] ??= 3);
        public int SelectedPageSizeIndex {
            get => _selectedPageSizeIndex;
            set {
                _selectedPageSizeIndex = value;
                ApplicationData.Current.LocalSettings.Values[nameof(SelectedPageSizeIndex)] = value;
                SetPages();
            }
        }

        [ObservableProperty]
        private List<Gallery> _filteredGalleries;
        partial void OnFilteredGalleriesChanged(List<Gallery> value) {
            SetPages();
        }

        [ObservableProperty]
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs = [];

        [RelayCommand]
        private void ExecuteQuery() {
            using HitomiContext context = new();
            IEnumerable<Gallery> filtered = [..
                context.Galleries
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
                filtered = filtered.Where(g => g.Title.Contains(QueryBuilderVM.SearchTitleText, StringComparison.CurrentCultureIgnoreCase));
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
    }
}
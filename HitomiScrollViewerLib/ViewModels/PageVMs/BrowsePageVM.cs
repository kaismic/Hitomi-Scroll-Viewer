﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class BrowsePageVM : DQObservableObject {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();
        private BrowsePageVM() {
            QueryBuilderVM.InsertTags([.. UserContext.Main.SavedBrowseTags]);
            QueryBuilderVM.TagCollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        foreach (Tag tag in e.NewItems) {
                            UserContext.Main.SavedBrowseTags.Add(tag);
                        }
                        UserContext.Main.SaveChanges();
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (Tag tag in e.OldItems) {
                            UserContext.Main.SavedBrowseTags.Remove(tag);
                        }
                        UserContext.Main.SaveChanges();
                        break;
                }
            };
            FilterGalleries();
            OnSelectedPageIndexChanged(0);
        }

        public int[] PageSizes { get; } = Enumerable.Range(1, 15).ToArray();

        [ObservableProperty]
        private List<int> _pages;

        [ObservableProperty]
        private int _selectedPageIndex = 0;
        partial void OnSelectedPageIndexChanged(int value) {
            CurrentGalleryBrowseItemVMs =
                FilteredGalleries
                .Skip(value * PageSizes[SelectedPageSizeIndex])
                .Take(PageSizes[SelectedPageSizeIndex])
                .Select(g => new GalleryBrowseItemVM() { Gallery = g })
                .ToList();
        }

        [ObservableProperty]
        private int _selectedPageSizeIndex = 4;
        partial void OnSelectedPageSizeIndexChanged(int value) {
            Pages = [.. Enumerable.Range(1, (int)Math.Ceiling((double)_filteredGalleryCount / PageSizes[value]))];
        }

        private int _filteredGalleryCount;

        [ObservableProperty]
        private IEnumerable<Gallery> _filteredGalleries;
        partial void OnFilteredGalleriesChanged(IEnumerable<Gallery> value) {
            _filteredGalleryCount = value.Count();
            SelectedPageIndex = 0;
        }

        [ObservableProperty]
        private List<GalleryBrowseItemVM> _currentGalleryBrowseItemVMs;

        public QueryBuilderVM QueryBuilderVM { get; } = new("BrowsePageGalleryLanguageIndex", "BrowsePageGalleryTypeIndex");

        [RelayCommand]
        private void FilterGalleries() {
            IEnumerable<Gallery> filtered = HitomiContext.Main.Galleries;
            if (QueryBuilderVM.GalleryLanguageSelectedIndex > 0 && QueryBuilderVM.SelectedGalleryLanguage is not null) {
                filtered = filtered.Intersect(QueryBuilderVM.SelectedGalleryLanguage.Galleries);
            }
            if (QueryBuilderVM.GalleryTypeSelectedIndex > 0 && QueryBuilderVM.SelectedGalleryTypeEntity is not null) {
                filtered = filtered.Intersect(QueryBuilderVM.SelectedGalleryTypeEntity.Galleries);
            }
            HashSet<Tag> currentTags = QueryBuilderVM.GetCurrentTags();
            if (currentTags.Count > 0) {
                foreach (Tag tag in currentTags) {
                    if (tag.Galleries is not null) {
                        filtered = filtered.Intersect(tag.Galleries);
                    }
                }
            }
            if (QueryBuilderVM.SearchTitleText.Length != 0) {
                filtered = filtered.Where(g => g.Title.Contains(QueryBuilderVM.SearchTitleText));
            }
            FilteredGalleries = filtered;
        }
    }
}
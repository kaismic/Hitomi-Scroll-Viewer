using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.UI;

namespace HitomiScrollViewerLib.ViewModels {
    public class QueryBuilderVM : IDisposable {
        public GalleryLanguage[] GalleryLanguages { get; }
        public GalleryTypeEntity[] GalleryTypes { get; }

        public TagTokenizingTextBoxVM[] TagTokenizingTBVMs { get; }
        public QueryConfiguration QueryConfiguration { get; }
        
        public event NotifyCollectionChangedEventHandler TagCollectionChanged;
        public event Action QueryChanged;

        private string _searchTitleText = "";
        public string SearchTitleText {
            get => _searchTitleText;
            set {
                _searchTitleText = value;
                QueryChanged?.Invoke();
            }
        }

        public bool AnyQuerySelected =>
            !QueryConfiguration.SelectedLanguage.IsAll ||
            QueryConfiguration.SelectedType.GalleryType != GalleryType.All ||
            SearchTitleText.Length > 0;

        private readonly HitomiContext _context = new();

        public QueryBuilderVM(PageKind pageKind) {
            GalleryLanguages = [.. _context.GalleryLanguages];
            GalleryTypes = [.. _context.GalleryTypes];
            QueryConfiguration = _context.QueryConfigurations.Find(pageKind);
            QueryConfiguration.SelectionChanged += () => QueryChanged?.Invoke();
            TagTokenizingTBVMs = [..
                Tag.TAG_CATEGORIES.Select(
                    category => new TagTokenizingTextBoxVM(_context.Tags, category)
                )
            ];
            foreach (var vm in TagTokenizingTBVMs) {
                vm.SelectedTags.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => TagCollectionChanged?.Invoke(sender, e);
            }
        }

        public void ClearSelectedTags() {
            foreach (var vm in TagTokenizingTBVMs) {
                vm.SelectedTags.Clear();
            }
        }

        public void InsertTags(ICollection<Tag> tags) {
            foreach (Tag tag in tags) {
                TagTokenizingTBVMs[(int)tag.Category].SelectedTags.Add(tag);
            }
        }

        public HashSet<Tag> GetCurrentTags() {
            return
                Enumerable
                .Range(0, Tag.TAG_CATEGORIES.Length)
                .Select(i => TagTokenizingTBVMs[i].SelectedTags)
                .SelectMany(tags => tags)
                .ToHashSet();
        }

        public void Dispose() {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

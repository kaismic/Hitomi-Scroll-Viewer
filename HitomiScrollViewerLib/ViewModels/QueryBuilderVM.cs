using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public class QueryBuilderVM {
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

        public QueryBuilderVM(HitomiContext context, QueryConfiguration queryConfig, GalleryLanguage[] languages, GalleryTypeEntity[] types) {
            QueryConfiguration = queryConfig;
            QueryConfiguration.SelectionChanged += () => QueryChanged?.Invoke();
            GalleryLanguages = languages;
            GalleryTypes = types;
            TagTokenizingTBVMs = [..Tag.TAG_CATEGORIES.Select(category => new TagTokenizingTextBoxVM(context, category))];
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
    }
}

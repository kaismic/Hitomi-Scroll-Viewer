using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerWebApp.Models {
    public class SearchFilterModel {
        private const string BASE_URL = "https://hitomi.la/";
        private const string SEARCH_PATH = "search.html?";
        public required GalleryLanguageDTO Language { get; init; }
        public required GalleryTypeDTO Type { get; init; }
        public required IEnumerable<TagDTO> IncludeTags { get; init; }
        public required IEnumerable<TagDTO> ExcludeTags { get; init; }
        public required string SearchKeywords { get; init; }

        private readonly List<LabeledTagCollection> _labeledTagCollections = [];
        public List<LabeledTagCollection> LabeledTagCollections {
            get {
                if (_labeledTagCollections == null) {
                    throw new InvalidOperationException($"{nameof(LabelTags)} must be called after creating {nameof(SearchFilterModel)} to set {nameof(LabeledTagCollections)}.");
                }
                return _labeledTagCollections;
            }
        }
        public void LabelTags() {
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                IEnumerable<TagDTO> includeTags = IncludeTags.Where(t => t.Category == category).OrderBy(t => t.Value);
                IEnumerable<TagDTO> excludeTags = ExcludeTags.Where(t => t.Category == category).OrderBy(t => t.Value);
                if (includeTags.Any() || excludeTags.Any()) {
                    LabeledTagCollections.Add(
                        new() {
                            Category = category,
                            IncludeTags = includeTags,
                            ExcludeTags = excludeTags
                        }
                    );
                }
            }
        }

        private string _searchLink = null!;
        public string SearchLink {
            get {
                if (_searchLink == null) {
                    throw new InvalidOperationException($"{nameof(BuildSearchLink)} must be called after creating {nameof(SearchFilterModel)} to set {nameof(SearchLink)}.");
                }
                return _searchLink;
            }
            private set => _searchLink = value;
        }
        public void BuildSearchLink() {
            List<string> searchParamStrs = new(5); // 5 = include + exclude + language, type and title search
            if (!Language.IsAll) {
                searchParamStrs.Add("language:" + Language.EnglishName);
            }
            if (!Type.IsAll) {
                searchParamStrs.Add("type:" + Type.Value);
            }
            // add joined include exclude search param strs
            searchParamStrs.Add(string.Join(' ', IncludeTags.Select(tag => tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue)));
            searchParamStrs.Add(string.Join(' ', ExcludeTags.Select(tag => '-' + tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue)));
            if (SearchKeywords.Length > 0) {
                searchParamStrs.Add(SearchKeywords);
            }
            string finalParamStr = string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s)));
            SearchLink = BASE_URL;
            if (finalParamStr.Length > 0) {
                SearchLink += SEARCH_PATH + finalParamStr;
            }
        }
    }
}
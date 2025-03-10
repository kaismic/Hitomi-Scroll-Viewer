using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Text;

namespace HitomiScrollViewerWebApp.Models {
    public class SearchFilterModel {
        private const string BASE_URL = "https://hitomi.la/";
        private const string SEARCH_PATH = "search.html?";
        public required GalleryLanguageDTO Language { get; init; }
        public required GalleryTypeDTO Type { get; init; }
        public required string SearchKeywordText { get; init; }

        private List<LabeledTagCollection>? _labeledTagCollections;
        public List<LabeledTagCollection> LabeledTagCollections {
            get {
                if (_labeledTagCollections == null) {
                    throw new InvalidOperationException($"{nameof(Init)} must be called after creating {nameof(SearchFilterModel)} to set {nameof(LabeledTagCollections)}.");
                }
                return _labeledTagCollections;
            }
            set => _labeledTagCollections = value;
        }
        public void Init(IEnumerable<TagDTO> includeTags, IEnumerable<TagDTO> excludeTags) {
            LabeledTagCollections = [];
            if (includeTags.Any() || excludeTags.Any()) {
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    IEnumerable<TagDTO> inc = includeTags.Where(t => t.Category == category).OrderBy(t => t.Value);
                    IEnumerable<TagDTO> exc = excludeTags.Where(t => t.Category == category).OrderBy(t => t.Value);
                    if (inc.Any() || exc.Any()) {
                        LabeledTagCollections.Add(
                            new() {
                                Category = category,
                                IncludeTags = inc,
                                ExcludeTags = exc
                            }
                        );
                    }
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
            StringBuilder sb = new();
            if (!Language.IsAll) {
                sb.Append("language:").Append(Language.EnglishName);
            }
            if (!Type.IsAll) {
                sb.Append("type:").Append(Type.Value);
            }
            foreach (LabeledTagCollection ltc in LabeledTagCollections) {
                sb.AppendJoin(' ', ltc.IncludeTags.Select(tag => tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue));
                sb.AppendJoin(' ', ltc.ExcludeTags.Select(tag => tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue));
            }
            if (SearchKeywordText.Length > 0) {
                sb.Append(SearchKeywordText);
            }
            SearchLink = BASE_URL;
            if (sb.Length > 0) {
                SearchLink += SEARCH_PATH + sb.ToString();
            }
        }
    }
}
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Text;

namespace HitomiScrollViewerData.Builders
{
    public class SearchFilterDTOBuilder
    {
        private const string BASE_URL = "https://hitomi.la/";
        private const string SEARCH_PATH = "search.html?";

        public required GalleryLanguageDTO Language { get; init; }
        public required GalleryTypeDTO Type { get; init; }
        public required string SearchKeywordText { get; init; }
        public required IEnumerable<TagDTO> IncludeTags { get; init; }
        public required IEnumerable<TagDTO> ExcludeTags { get; init; }

        public SearchFilterDTO Build() {
            List<LabeledTagCollectionDTO> labeledTagCollections = [];
            if (IncludeTags.Any() || ExcludeTags.Any()) {
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    ICollection<TagDTO> inc = [.. IncludeTags.Where(t => t.Category == category).OrderBy(t => t.Value)];
                    ICollection<TagDTO> exc = [.. ExcludeTags.Where(t => t.Category == category).OrderBy(t => t.Value)];
                    if (inc.Count > 0 || exc.Count > 0) {
                        labeledTagCollections.Add(
                            new() {
                                Category = category,
                                IncludeTags = inc,
                                ExcludeTags = exc
                            }
                        );
                    }
                }
            }

            StringBuilder sb = new();
            if (!Language.IsAll) {
                sb.Append("language:" + Language.EnglishName);
            }
            if (!Type.IsAll) {
                sb.Append("type:" + Type.Value);
            }
            foreach (LabeledTagCollectionDTO ltc in labeledTagCollections) {
                sb.AppendJoin(' ', ltc.IncludeTags.Select(tag => tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue));
                sb.AppendJoin(' ', ltc.ExcludeTags.Select(tag => tag.Category.ToString().ToLower() + ':' + tag.SearchParamValue));
            }
            if (SearchKeywordText.Length > 0) {
                sb.Append(SearchKeywordText);
            }
            string searchLink;
            if (sb.Length > 0) {
                searchLink = BASE_URL + SEARCH_PATH + sb.ToString();
            } else {
                searchLink = BASE_URL;
            }

            return new() {
                LabeledTagCollections = labeledTagCollections,
                Language = Language,
                Type = Type,
                SearchKeywordText = SearchKeywordText,
                SearchLink = searchLink
            };
        }
    }
}

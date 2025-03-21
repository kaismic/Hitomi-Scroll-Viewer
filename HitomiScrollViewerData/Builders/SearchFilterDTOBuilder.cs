﻿using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Web;

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
                                IncludeTagValues = inc.Select(t => t.Value),
                                ExcludeTagValues = exc.Select(t => t.Value)
                            }
                        );
                    }
                }
            }

            List<string> searchParams = [];
            if (!Language.IsAll) {
                searchParams.Add("language:" + Language.EnglishName);
            }
            if (!Type.IsAll) {
                searchParams.Add("type:" + Type.Value);
            }
            foreach (LabeledTagCollectionDTO ltc in labeledTagCollections) {
                if (ltc.IncludeTagValues.Any()) {
                    searchParams.Add(string.Join(' ', ltc.IncludeTagValues.Select(v => ltc.Category.ToString().ToLower() + ':' + v.Replace(' ', '_'))));
                }
                if (ltc.ExcludeTagValues.Any()) {
                    searchParams.Add(string.Join(' ', ltc.ExcludeTagValues.Select(v => '-' + ltc.Category.ToString().ToLower() + ':' + v.Replace(' ', '_'))));
                }
            }
            if (SearchKeywordText.Length > 0) {
                searchParams.Add(SearchKeywordText);
            }
            string searchLink;
            if (searchParams.Count > 0) {
                searchLink = BASE_URL + SEARCH_PATH + string.Join(' ', searchParams);
            } else {
                searchLink = BASE_URL;
            }

            return new() {
                LabeledTagCollections = labeledTagCollections,
                Language = Language,
                Type = Type,
                SearchKeywordText = SearchKeywordText,
                SearchLink = HttpUtility.UrlPathEncode(searchLink)
            };
        }
    }
}

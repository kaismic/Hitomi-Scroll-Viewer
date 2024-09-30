using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public class SearchFilterVM {
        private static readonly ResourceMap _tagCategoryRM = MainResourceMap.GetSubtree(nameof(TagCategory));
        private const string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Dictionary<TagCategory, string> CATEGORY_SEARCH_PARAM_DICT = new() {
            { TagCategory.Tag, "tag" },
            { TagCategory.Male, "male" },
            { TagCategory.Female, "female" },
            { TagCategory.Artist, "artist" },
            { TagCategory.Group, "group" },
            { TagCategory.Character, "character" },
            { TagCategory.Series, "series" }
        };

        public GalleryTypeEntity GalleryType { get; set; }
        public GalleryLanguage GalleryLanguage { get; set; }
        public string SearchTitleText { get; set; }

        public HashSet<Tag> IncludeTags { get; init; }
        public HashSet<Tag> ExcludeTags { get; init; }

        public List<InExcludeTagCollection> InExcludeTagCollections { get; } = [];

        public StandardUICommand DeleteCommand { get; } = new(StandardUICommandKind.Delete) {
            IconSource = new SymbolIconSource() { Symbol = Symbol.Delete }
        };

        private Uri _searchLink;
        public Uri SearchLink {
            get {
                if (_searchLink != null) {
                    return _searchLink;
                }

                List<string> searchParamStrs = new(5); // 5 = include + exclude + language, type and title search

                if (GalleryLanguage != null) {
                    searchParamStrs.Add("language:" + GalleryLanguage.SearchParamValue);
                }
                if (GalleryType != null) {
                    searchParamStrs.Add("type:" + GalleryType.SearchParamValue);
                }

                // add joined include exclude search param strs
                searchParamStrs.Add(string.Join(' ', IncludeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
                searchParamStrs.Add(string.Join(' ', ExcludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

                // add display texts for each tag category
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    HashSet<string> includeValues = IncludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value).ToHashSet();
                    HashSet<string> excludeValues = ExcludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value).ToHashSet();
                    if (includeValues.Count == 0 && excludeValues.Count == 0) {
                        continue;
                    }
                    string[] withoutEmptyStrs = new string[] {
                        string.Join(", ", includeValues),
                        string.Join(", ", excludeValues)
                    }.Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
                }

                if (SearchTitleText != null) {
                    searchParamStrs.Add(SearchTitleText);
                }

                return _searchLink = new Uri(SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s))));
            }
        }

        public void InitInExcludeTagCollections() {
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                List<Tag> includeTags = Tag.SelectTagsFromCategory(IncludeTags, category);
                List<Tag> excludeTags = Tag.SelectTagsFromCategory(ExcludeTags, category);
                if (includeTags.Count > 0 || excludeTags.Count > 0) {
                    InExcludeTagCollections.Add(
                        new() {
                            CategoryLabel = _tagCategoryRM.GetValue(category.ToString()).ValueAsString,
                            IncludeTags = includeTags,
                            ExcludeTags = excludeTags
                        }
                    );
                }
            }
        }
    }
}

using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels {
    public class SearchFilterVM {
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

        public GalleryTypeEntity GalleryType { get; init; }
        public GalleryLanguage GalleryLanguage { get; init; }
        public string SearchTitleText { get; init; }

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

                if (!GalleryLanguage.IsAll) {
                    searchParamStrs.Add("language:" + GalleryLanguage.SearchParamValue);
                }
                if (GalleryType.GalleryType != Entities.GalleryType.All) {
                    searchParamStrs.Add("type:" + GalleryType.SearchParamValue);
                }

                // add joined include exclude search param strs
                searchParamStrs.Add(string.Join(' ', IncludeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
                searchParamStrs.Add(string.Join(' ', ExcludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

                if (SearchTitleText.Length > 0) {
                    searchParamStrs.Add(SearchTitleText);
                }

                return _searchLink = new Uri(SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s))));
            }
        }

        public void InitInExcludeTagCollections() {
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                ICollection<Tag> includeTags = Tag.SelectTagsFromCategory(IncludeTags, category);
                ICollection<Tag> excludeTags = Tag.SelectTagsFromCategory(ExcludeTags, category);
                if (includeTags.Count > 0 || excludeTags.Count > 0) {
                    InExcludeTagCollections.Add(
                        new() {
                            CategoryLabel = category.ToString().GetLocalized(nameof(TagCategory)),
                            IncludeTags = includeTags,
                            ExcludeTags = excludeTags
                        }
                    );
                }
            }
        }
    }
}

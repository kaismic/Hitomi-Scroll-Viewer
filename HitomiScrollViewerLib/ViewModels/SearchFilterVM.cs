using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
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

        private GalleryTypeEntity _galleryType;
        public GalleryTypeEntity GalleryType {
            get => _galleryType;
            set {
                _galleryType = value;
                GalleryTypeText = TEXT_TYPE + ": " + value.DisplayName;
            }
        }
        public string GalleryTypeText { get; private set; }

        private GalleryLanguage _galleryLanguage;
        public GalleryLanguage GalleryLanguage {
            get => _galleryLanguage;
            set {
                _galleryLanguage = value;
                GalleryLanguageText = TEXT_LANGUAGE + ": " + GalleryLanguage.LocalName;
            }
        }
        public string GalleryLanguageText { get; private set; }

        public string SearchTitleText { get; set; }

        public IEnumerable<Tag> IncludeTags { get; set; }
        public IEnumerable<Tag> ExcludeTags { get; set; }

        public List<SearchFilterTagsRepeaterVM> SearchFilterTagsRepeaterVMs { get; } = [];

        public StandardUICommand DeleteCommand { get; } = new(StandardUICommandKind.Delete);

        private string _searchLink;
        public string SearchLink {
            get {
                if (_searchLink != null) {
                    return _searchLink;
                }

                List<string> searchParamStrs = new(5); // 5 = include + exclude + language, type and title search
                List<string> displayTexts = new(Tag.TAG_CATEGORIES.Length + 3); // + 3 for language, type and title search

                if (GalleryLanguage != null) {
                    searchParamStrs.Add("language:" + GalleryLanguage.SearchParamValue);
                    displayTexts.Add(TEXT_LANGUAGE + ": " + GalleryLanguage.LocalName);
                }

                if (GalleryType != null) {
                    searchParamStrs.Add("type:" + GalleryType.SearchParamValue);
                    displayTexts.Add(TEXT_TYPE + ": " + GalleryType.DisplayName);
                }

                // add joined include exclude search param strs
                searchParamStrs.Add(string.Join(' ', IncludeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
                searchParamStrs.Add(string.Join(' ', ExcludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

                // add display texts for each tag category
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    IEnumerable<string> includeValues = IncludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                    IEnumerable<string> excludeValues = ExcludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                    if (!includeValues.Any() && !excludeValues.Any()) {
                        continue;
                    }
                    IEnumerable<string> withoutEmptyStrs = new string[] {
                    string.Join(", ", includeValues),
                    string.Join(", ", excludeValues)
                }.Where(s => !string.IsNullOrEmpty(s));
                    displayTexts.Add((category).ToString() + ": " + string.Join(", ", withoutEmptyStrs));
                }

                if (SearchTitleText != null) {
                    searchParamStrs.Add(SearchTitleText);
                    displayTexts.Add(SearchTitleText);
                }

                return _searchLink = SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public void InitSearchFilterTagsRepeaterVMs() {
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                List<Tag> includeTags = Tag.GetTagsByCategory(IncludeTags, category);
                List<Tag> excludeTags = Tag.GetTagsByCategory(ExcludeTags, category);
                if (includeTags.Count > 0 || excludeTags.Count > 0) {
                    SearchFilterTagsRepeaterVMs.Add(
                        new() {
                            CategoryLabel = _tagCategoryRM.GetValue(category.ToString()).ValueAsString,
                            IncludeTags = includeTags,
                            ExcludeTags = excludeTags
                        }
                    );
                }
            }
        }

        public IEnumerable<Gallery> GetFilteredGalleries() {
            IEnumerable<Gallery> filtered = HitomiContext.Main.Galleries;
            foreach (Tag includeTag in IncludeTags) {
                filtered = filtered.Intersect(includeTag.Galleries);
            }
            foreach (Tag excludeTag in ExcludeTags) {
                filtered = filtered.Except(excludeTag.Galleries);
            }
            if (GalleryLanguage != null) {
                filtered = filtered.Where(g => g.GalleryLanguage.Id == GalleryLanguage.Id);
            }
            if (GalleryType != null) {
                filtered = filtered.Where(g => g.GalleryType.Id == GalleryType.Id);
            }
            if (SearchTitleText != null) {
                filtered = filtered.Where(g => g.Title.Contains(SearchTitleText));
            }
            return filtered;
        }
    }
}

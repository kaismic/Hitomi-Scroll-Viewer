using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class QueryBuilderVM : DQObservableObject {
        public static readonly GalleryLanguage[] GALLERY_LANGUAGES = GetGalleryLanguages();
        public static readonly GalleryTypeEntity[] GALLERY_TYPE_ENTITIES = GetGalleryTypes();
        private static GalleryLanguage[] GetGalleryLanguages() {
            using HitomiContext context = new();
            return [
                new GalleryLanguage() { LocalName = TEXT_ALL },
                .. context.GalleryLanguages.AsNoTracking().OrderBy(gl => gl.LocalName)
            ];
        }
        private static GalleryTypeEntity[] GetGalleryTypes() {
            using HitomiContext context = new();
            return [
                new GalleryTypeEntity() { GalleryType = null },
                .. context.GalleryTypes.AsNoTracking()
            ];
        }


        public TagTokenizingTextBoxVM[] TagTokenizingTBVMs { get; }
        public event NotifyCollectionChangedEventHandler TagCollectionChanged;

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex = -1;
        partial void OnGalleryLanguageSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[_galleryLanguageIndexSettingsKey] = value;
            SelectedGalleryLanguage = GALLERY_LANGUAGES[value];
            QueryParameterChanged?.Invoke();
        }
        public GalleryLanguage SelectedGalleryLanguage { get; private set; }

        [ObservableProperty]
        private int _galleryTypeSelectedIndex = -1;
        partial void OnGalleryTypeSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[_galleryTypeIndexSettingsKey] = value;
            SelectedGalleryTypeEntity = GALLERY_TYPE_ENTITIES[value];
            QueryParameterChanged?.Invoke();
        }
        public GalleryTypeEntity SelectedGalleryTypeEntity { get; private set; }

        [ObservableProperty]
        private string _searchTitleText = "";
        partial void OnSearchTitleTextChanged(string value) {
            QueryParameterChanged?.Invoke();
        }

        private readonly string _galleryLanguageIndexSettingsKey;
        private readonly string _galleryTypeIndexSettingsKey;

        public event Action QueryParameterChanged;
        public bool AnyQuerySelected =>
            GalleryLanguageSelectedIndex > 0 ||
            GalleryTypeSelectedIndex > 0 ||
            SearchTitleText.Length > 0;

        public QueryBuilderVM(HitomiContext context, string galleryLanguageIndexSettingsKey, string galleryTypeIndexSettingsKey) {
            _galleryLanguageIndexSettingsKey = galleryLanguageIndexSettingsKey;
            _galleryTypeIndexSettingsKey = galleryTypeIndexSettingsKey;
            GalleryLanguageSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[galleryLanguageIndexSettingsKey] ??= 0);
            GalleryTypeSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[galleryTypeIndexSettingsKey] ??= 0);

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

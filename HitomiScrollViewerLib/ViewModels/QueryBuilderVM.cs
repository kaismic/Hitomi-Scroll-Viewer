using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class QueryBuilderVM : DQObservableObject {
        public static readonly GalleryLanguage[] GALLERY_LANGUAGES =
            [new GalleryLanguage() { LocalName = TEXT_ALL }, .. HitomiContext.Main.GalleryLanguages.OrderBy(gl => gl.LocalName)];
        public static readonly GalleryTypeEntity[] GALLERY_TYPE_ENTITIES =
            [new GalleryTypeEntity() { GalleryType = null }, .. HitomiContext.Main.GalleryTypes];

        public TagTokenizingTextBoxVM[] TagTokenizingTBVMs { get; } = [.. Tag.TAG_CATEGORIES.Select(category => new TagTokenizingTextBoxVM(category))];
        public event NotifyCollectionChangedEventHandler TagCollectionChanged;

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex;
        partial void OnGalleryLanguageSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[_galleryLanguageIndexSettingsKey] = value;
            SelectedGalleryLanguage = GALLERY_LANGUAGES[value];
        }
        public GalleryLanguage SelectedGalleryLanguage { get; private set; }

        [ObservableProperty]
        private int _galleryTypeSelectedIndex;
        partial void OnGalleryTypeSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[_galleryTypeIndexSettingsKey] = value;
            SelectedGalleryTypeEntity = GALLERY_TYPE_ENTITIES[value];
        }
        public GalleryTypeEntity SelectedGalleryTypeEntity { get; private set; }

        [ObservableProperty]
        private string _searchTitleText = "";

        private readonly string _galleryLanguageIndexSettingsKey;
        private readonly string _galleryTypeIndexSettingsKey;

        public QueryBuilderVM(string galleryLanguageIndexSettingsKey, string galleryTypeIndexSettingsKey) {
            _galleryLanguageIndexSettingsKey = galleryLanguageIndexSettingsKey;
            _galleryTypeIndexSettingsKey = galleryTypeIndexSettingsKey;
            GalleryLanguageSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[galleryLanguageIndexSettingsKey] ??= 0);
            GalleryTypeSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[galleryTypeIndexSettingsKey] ??= 0);

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

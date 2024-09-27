using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class QueryBuilderVM : DQObservableObject {
        public static readonly GalleryTypeEntity[] GALLERY_TYPE_ENTITIES =
            [new GalleryTypeEntity() { GalleryType = null }, .. HitomiContext.Main.GalleryTypes];
        public static readonly GalleryLanguage[] GALLERY_LANGUAGES =
            [new GalleryLanguage() { LocalName = TEXT_ALL }, .. HitomiContext.Main.GalleryLanguages.OrderBy(gl => gl.LocalName)];

        public TagTokenizingTextBoxVM[] TagTokenizingTBVMs { get; } = [.. Tag.TAG_CATEGORIES.Select(category => new TagTokenizingTextBoxVM(category))];
        
        [ObservableProperty]
        private int _galleryTypeSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[nameof(GalleryTypeSelectedIndex)] ??= 0);
        partial void OnGalleryTypeSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[nameof(GalleryTypeSelectedIndex)] = value;
            SelectedGalleryTypeEntity = GALLERY_TYPE_ENTITIES[value];
        }
        public GalleryTypeEntity SelectedGalleryTypeEntity { get; private set; }

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex = (int)(ApplicationData.Current.LocalSettings.Values[nameof(GalleryLanguageSelectedIndex)] ??= 0);
        partial void OnGalleryLanguageSelectedIndexChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[nameof(GalleryLanguageSelectedIndex)] = value;
            SelectedGalleryLanguage = GALLERY_LANGUAGES[value];
        }
        public GalleryLanguage SelectedGalleryLanguage { get; private set; }

        [ObservableProperty]
        private string _searchTitleText = "";

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

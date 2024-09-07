using HitomiScrollViewerLib.DbContexts;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public class OriginalGalleryInfo {
        private static readonly Dictionary<TagCategory, string> CATEGORY_PROP_KEY_DICT = new() {
            { TagCategory.Tag, "tag" },
            { TagCategory.Male, "male" },
            { TagCategory.Female, "female" },
            { TagCategory.Artist, "artist" },
            { TagCategory.Group, "group" },
            { TagCategory.Character, "character" },
            { TagCategory.Series, "parody" }
        };

        public int Id { get; set; }
        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public string Language { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string LanguageUrl { get; set; }
        public string LanguageLocalname { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        public ImageInfo[] Files { get; set; }
        public Dictionary<string, string>[] Artists { get; set; }
        public Dictionary<string, string>[] Groups { get; set; }
        public Dictionary<string, string>[] Characters { get; set; }
        public Dictionary<string, string>[] Parodys { get; set; }
        public CompositeTag[] Tags { get; set; }

        public struct CompositeTag {
            public string Tag { get; set; }
            public int? Male { get; set; }
            public int? Female { get; set; }
        }

        public Gallery ToGallery() {
            Gallery gallery = new() {
                Id = Id,
                Title = Title,
                JapaneseTitle = JapaneseTitle,
                GalleryLanguage = HitomiContext.Main.GalleryLanguages.First(l => l.SearchParamValue == Language),
                GalleryType = HitomiContext.Main.GalleryTypes.First(t => t.SearchParamValue == Type),
                Date = Date,
                SceneIndexes = SceneIndexes,
                Related = Related,
                Files =
                    Files,
                    //.Select((imageInfo, i) => {
                    //    ImageInfo clone = imageInfo.Clone();
                    //    clone.Index = i;
                    //    return clone;
                    //})
                    //.ToHashSet(),
                    // TODO test if above is enough
                Tags = []
            };
            SetGalleryProperty(Artists, gallery, TagCategory.Artist);
            SetGalleryProperty(Groups, gallery, TagCategory.Group);
            SetGalleryProperty(Characters, gallery, TagCategory.Character);
            SetGalleryProperty(Parodys, gallery, TagCategory.Series);

            foreach (var compositeTag in Tags) {
                gallery.Tags.Add(Tag.GetTag(
                    compositeTag.Tag,
                    compositeTag.Male == 1   ? TagCategory.Male   :
                    compositeTag.Female == 1 ? TagCategory.Female :
                                               TagCategory.Tag
                ));
            }

            return gallery;
        }

        private static void SetGalleryProperty(
            Dictionary<string, string>[] originalDictArr,
            Gallery gallery,
            TagCategory category
        ) {
            if (originalDictArr != null) {
                foreach (var dict in originalDictArr) {
                    gallery.Tags.Add(Tag.GetTag(dict[CATEGORY_PROP_KEY_DICT[category]], category));
                }
            }
        }
    }
}

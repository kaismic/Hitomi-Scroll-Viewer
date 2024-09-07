using HitomiScrollViewerLib.DbContexts;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public class OriginalGalleryInfo {
        private static readonly Dictionary<Category, string> CATEGORY_PROP_KEY_DICT = new() {
            { Category.Tag, "tag" },
            { Category.Male, "male" },
            { Category.Female, "female" },
            { Category.Artist, "artist" },
            { Category.Group, "group" },
            { Category.Character, "character" },
            { Category.Series, "parody" }
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
            SetGalleryProperty(Artists, gallery, Category.Artist);
            SetGalleryProperty(Groups, gallery, Category.Group);
            SetGalleryProperty(Characters, gallery, Category.Character);
            SetGalleryProperty(Parodys, gallery, Category.Series);

            foreach (var compositeTag in Tags) {
                gallery.Tags.Add(Tag.GetTag(
                    compositeTag.Tag,
                    compositeTag.Male == 1   ? Category.Male   :
                    compositeTag.Female == 1 ? Category.Female :
                                               Category.Tag
                ));
            }

            return gallery;
        }

        private static void SetGalleryProperty(
            Dictionary<string, string>[] originalDictArr,
            Gallery gallery,
            Category category
        ) {
            if (originalDictArr != null) {
                foreach (var dict in originalDictArr) {
                    gallery.Tags.Add(Tag.GetTag(dict[CATEGORY_PROP_KEY_DICT[category]], category));
                }
            }
        }
    }
}

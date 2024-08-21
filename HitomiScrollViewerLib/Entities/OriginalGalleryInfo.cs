using HitomiScrollViewerLib.DbContexts;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public class OriginalGalleryInfo {
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
                Language = Language,
                Type = Type,
                Date = Date,
                LanguageUrl = LanguageUrl,
                LanguageLocalname = LanguageLocalname,
                SceneIndexes = SceneIndexes,
                Related = Related,
                Files = Files, // TODO test this but I'm pretty sure it won't work and will have to create a new instance
                Tags = []
            };
            SetGalleryProperty(Artists, gallery, Category.Artist);
            SetGalleryProperty(Groups, gallery, Category.Group);
            SetGalleryProperty(Characters, gallery, Category.Character);
            SetGalleryProperty(Parodys, gallery, Category.Series);

            foreach (var compositeTag in Tags) {
                if (compositeTag.Male == 1) {
                    gallery.Tags.Add(
                        HitomiContext.Main.Tags
                            .Where(tag => tag.Value == compositeTag.Tag && tag.Category == Category.Male)
                            .First()
                    );
                } else if (compositeTag.Female == 1) {
                    gallery.Tags.Add(
                        HitomiContext.Main.Tags
                            .Where(tag => tag.Value == compositeTag.Tag && tag.Category == Category.Female)
                            .First()
                    );
                } else {
                    gallery.Tags.Add(
                        HitomiContext.Main.Tags
                            .Where(tag => tag.Value == compositeTag.Tag && tag.Category == Category.Tag)
                            .First()
                    );
                }
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
                    gallery.Tags.Add(
                        HitomiContext.Main.Tags
                            .Where(tag => tag.Value == dict[Tag.CATEGORY_PROP_KEY_MAP[category]] && tag.Category == category)
                            .First()
                    );
                }
            }
        }
    }
}

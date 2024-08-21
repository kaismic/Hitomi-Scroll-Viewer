using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities.Tags;
using Microsoft.EntityFrameworkCore;
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
            };
            if (Artists != null) {
                gallery.ArtistTags = [];
                SetGalleryProperty(Artists, (ICollection<TagBase>)gallery.ArtistTags, (DbSet<TagBase>)(object)HitomiContext.Main.ArtistTags, "artist");
            }
            if (Groups != null) {
                gallery.GroupTags = [];
                SetGalleryProperty(Groups, (ICollection<TagBase>)gallery.GroupTags, (DbSet<TagBase>)(object)HitomiContext.Main.GroupTags, "group");
            }
            if (Characters != null) {
                gallery.CharacterTags = [];
                SetGalleryProperty(Characters, (ICollection<TagBase>)gallery.CharacterTags, (DbSet<TagBase>)(object)HitomiContext.Main.CharacterTags, "character");
            }
            if (Parodys != null) {
                gallery.CharacterTags = [];
                SetGalleryProperty(Parodys, (ICollection<TagBase>)gallery.CharacterTags, (DbSet<TagBase>)(object)HitomiContext.Main.CharacterTags, "parody");
            }

            gallery.MaleTags = [];
            gallery.FemaleTags = [];
            gallery.TagTags = [];
            foreach (var compositeTag in Tags) {
                if (compositeTag.Male == 1) {
                    gallery.MaleTags.Add(
                        HitomiContext.Main.MaleTags
                            .Where(tag => tag.Value == compositeTag.Tag)
                            .First()
                    );
                } else if (compositeTag.Female == 1) {
                    gallery.FemaleTags.Add(
                        HitomiContext.Main.FemaleTags
                            .Where(tag => tag.Value == compositeTag.Tag)
                            .First()
                    );
                } else {
                    gallery.TagTags.Add(
                        HitomiContext.Main.TagTags
                            .Where(tag => tag.Value == compositeTag.Tag)
                            .First()
                    );
                }
            }

            return gallery;
        }

        private static void SetGalleryProperty(
            Dictionary<string, string>[] originalDictArr,
            ICollection<TagBase> galleryTagCollection,
            DbSet<TagBase> mainCtxDbSet,
            string propertyKey
        ) {
            foreach (var dict in originalDictArr) {
                galleryTagCollection.Add(
                    mainCtxDbSet
                        .Where(tag => tag.Value == dict[propertyKey])
                        .First()
                );
            }
        }
    }
}

using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.DTOs
{
    public class OriginalGalleryInfoDTO
    {
        public static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
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
        [JsonConverter(typeof(GalleryDateTimeOffsetConverter))]
        public DateTimeOffset Date { get; set; }
        public string LanguageUrl { get; set; }
        public string LanguageLocalname { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        public ICollection<OriginalImageInfoDTO> Files { get; set; }
        public Dictionary<string, string>[] Artists { get; set; }
        public Dictionary<string, string>[] Groups { get; set; }
        public Dictionary<string, string>[] Characters { get; set; }
        public Dictionary<string, string>[] Parodys { get; set; }
        public CompositeTag[] Tags { get; set; }

        public struct CompositeTag
        {
            public string Tag { get; set; }
            [JsonConverter(typeof(EmptyStringNumberJsonConverter))]
            public int Male { get; set; }
            [JsonConverter(typeof(EmptyStringNumberJsonConverter))]
            public int Female { get; set; }
        }

        private class EmptyStringNumberJsonConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                try
                {
                    string s = reader.GetString();
                    if (s.Length == 0)
                    {
                        return 0;
                    }
                    return int.Parse(s);
                }
                catch (InvalidOperationException) { }
                return 0;
            }
            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
        }

        private class GalleryDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
        {
            public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTimeOffset.Parse(reader.GetString());
            }
            public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mmzzz"));
            }
        }

        private static void SetGalleryProperty(
            HitomiContext context,
            Dictionary<string, string>[] originalDictArr,
            List<Tag> tags,
            TagCategory category
        )
        {
            if (originalDictArr != null)
            {
                foreach (var dict in originalDictArr)
                {
                    tags.Add(Tag.GetTag(context.Tags, dict[CATEGORY_PROP_KEY_DICT[category]], category));
                }
            }
        }

        public Gallery ToGallery(HitomiContext context)
        {
            List<Tag> tags = [];
            SetGalleryProperty(context, Artists, tags, TagCategory.Artist);
            SetGalleryProperty(context, Groups, tags, TagCategory.Group);
            SetGalleryProperty(context, Characters, tags, TagCategory.Character);
            SetGalleryProperty(context, Parodys, tags, TagCategory.Series);

            foreach (var compositeTag in Tags)
            {
                tags.Add(Tag.GetTag(
                    context.Tags,
                    compositeTag.Tag,
                    compositeTag.Male == 1 ? TagCategory.Male :
                    compositeTag.Female == 1 ? TagCategory.Female :
                                               TagCategory.Tag
                ));
            }
            
            Gallery gallery = new()
            {
                Id = Id,
                Title = Title,
                JapaneseTitle = JapaneseTitle,
                GalleryLanguage = context.GalleryLanguages.First(l => l.SearchParamValue == Language),
                GalleryType = context.GalleryTypes.First(t => t.SearchParamValue == Type),
                Date = Date,
                SceneIndexes = SceneIndexes,
                Related = Related,
                LastDownloadTime = DateTime.UtcNow,
                Files = [.. Files.Select(f => f.ToImageInfo())],
                Tags = tags
            };

            return gallery;
        }
    }
}

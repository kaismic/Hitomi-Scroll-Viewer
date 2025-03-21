using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerData.DTOs {
    public class OriginalGalleryInfoDTO {
        public static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
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
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public required string Language { get; set; }
        public required string Type { get; set; }
        [JsonConverter(typeof(GalleryDateTimeOffsetConverter))]
        public DateTimeOffset Date { get; set; }
        public required string LanguageUrl { get; set; }
        public required string LanguageLocalname { get; set; }
        public required int[] SceneIndexes { get; set; }
        public required int[] Related { get; set; }
        public required ICollection<OriginalImageInfoDTO> Files { get; set; }
        public Dictionary<string, string>[]? Artists { get; set; }
        public Dictionary<string, string>[]? Groups { get; set; }
        public Dictionary<string, string>[]? Characters { get; set; }
        public Dictionary<string, string>[]? Parodys { get; set; }
        public required CompositeTag[] Tags { get; set; }

        public struct CompositeTag {
            public string Tag { get; set; }
            [JsonConverter(typeof(EmptyStringNumberJsonConverter))]
            public int Male { get; set; }
            [JsonConverter(typeof(EmptyStringNumberJsonConverter))]
            public int Female { get; set; }
        }

        private class EmptyStringNumberJsonConverter : JsonConverter<int> {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                try {
                    string s = reader.GetString()!;
                    if (s.Length == 0) {
                        return 0;
                    }
                    return int.Parse(s);
                } catch (InvalidOperationException) {
                    return reader.GetInt32();
                }
            }
            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
        }

        private class GalleryDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {
            public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                return DateTimeOffset.Parse(reader.GetString()!);
            }
            public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mmzzz"));
            }
        }

        private static void SetGalleryPropertyAsync(
            HitomiContext context,
            Dictionary<string, string>[]? originalDictArr,
            List<Tag> tags,
            TagCategory category
        ) {
            if (originalDictArr != null) {
                foreach (var dict in originalDictArr) {
                    string tagValue = dict[CATEGORY_PROP_KEY_DICT[category]];
                    Tag? tag = Utils.GetTag(context.Tags, tagValue, category);
                    if (tag == null) {
                        //await Utils.FetchAndUpdateTagsAsync(context, category, tagValue);
                        tag = Utils.GetTag(context.Tags, tagValue, category);
                        if (tag == null) {
                            Debug.WriteLine($"Could not fetch Tag: Value = {tagValue}, Category: {category}");
                        } else {
                            tags.Add(tag);
                        }
                    } else {
                        tags.Add(tag);
                    }
                }
            }
        }

        public Gallery ToGallery(HitomiContext context) {
            List<Tag> tags = [];
            SetGalleryPropertyAsync(context, Artists, tags, TagCategory.Artist);
            SetGalleryPropertyAsync(context, Groups, tags, TagCategory.Group);
            SetGalleryPropertyAsync(context, Characters, tags, TagCategory.Character);
            SetGalleryPropertyAsync(context, Parodys, tags, TagCategory.Series);

            foreach (var compositeTag in Tags) {
                tags.Add(Utils.GetTag(
                    context.Tags,
                    compositeTag.Tag,
                    compositeTag.Male == 1 ? TagCategory.Male :
                    compositeTag.Female == 1 ? TagCategory.Female :
                                               TagCategory.Tag
                )!);
            }

            // appends leading zeros
            string indexFormat = "D" + Math.Floor(Math.Log10(Files.Count) + 1);
            Gallery gallery = new() {
                Id = Id,
                Title = Title,
                JapaneseTitle = JapaneseTitle,
                GalleryLanguage = context.GalleryLanguages.First(l => l.EnglishName == Language),
                GalleryType = context.GalleryTypes.First(t => t.Value == Type),
                Date = Date,
                SceneIndexes = SceneIndexes,
                Related = Related,
                LastDownloadTime = DateTime.UtcNow,
                Files = [..
                    Files.Select(
                        (f, i) => {
                            ImageInfo imageInfo = f.ToImageInfo();
                            imageInfo.Index = i + 1;
                            imageInfo.FileName = imageInfo.Index.ToString(indexFormat);
                            return imageInfo;
                        }
                    )
                ],
                Tags = tags
            };

            return gallery;
        }
    }
}

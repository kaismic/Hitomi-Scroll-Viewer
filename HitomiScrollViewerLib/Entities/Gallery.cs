using HitomiScrollViewerLib.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.Entities
{
    [Index(nameof(Title))]
    [Index(nameof(Type))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public int[] SceneIndexes { get; set; }

        // TODO modify
        private class EntityWithUrlConverter(string targetKey) : JsonConverter<ICollection<TagBase>> {
            public override ICollection<TagBase> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                // TODO test this
                try {
                    JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeToConvert));
                }
                ICollection<TagBase> result = [];
                if (reader.TokenType != JsonTokenType.StartArray) return [.. result];
                while (reader.Read()) {
                    switch (reader.TokenType) {
                        case JsonTokenType.StartObject:
                            break;
                        case JsonTokenType.String:
                            // already converted
                            while (reader.TokenType == JsonTokenType.String) {
                                result.Add(reader.GetString());
                                reader.Read();
                            }
                            return [.. result];
                        default:
                            return [.. result];
                    }
                    if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) return [.. result];
                    if (!reader.Read() || reader.TokenType != JsonTokenType.String) return [.. result];
                    result.Add(reader.GetString());
                    if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) return [.. result];
                    if (!reader.Read() || reader.TokenType != JsonTokenType.String) return [.. result];
                    if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject) return [.. result];
                }
                return [.. result];
            }

            public override void Write(Utf8JsonWriter writer, ICollection<TagBase> value, JsonSerializerOptions options) {
                writer.WriteStartArray();
                foreach (TagBase artist in value) {
                    writer.WriteStringValue(artist);
                }
                writer.WriteEndArray();
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonConverterWithTargetKeyAttribute(Type converterType, string targetKey) : JsonConverterAttribute {

            // CreateConverter method is only called if base.ConverterType is null 
            // so store the type parameter in a new property in the derived attribute
            // https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/JsonSerializerOptions.Converters.cs#L278
            public new Type ConverterType { get; } = converterType;

            public override JsonConverter CreateConverter(Type _) {
                return (JsonConverter)Activator.CreateInstance(ConverterType, targetKey);
            }
        }

        // Example: "artists":[{"artist":"hsd","url":"/artist/hsd-all.html"}, {"artist":"nora_higuma","url":"/artist/nora%20higuma-all.html"}]
        // After conversion: "artists":["hsd", "nora_higuma"]
        [JsonConverterWithTargetKey(typeof(EntityWithUrlConverter), "artist")]
        public virtual ICollection<TagBase> Artists { get; set; }

        public virtual ICollection<TagTag> TagTags { get; set; }
        public virtual ICollection<MaleTag> MaleTags { get; set; }
        public virtual ICollection<FemaleTag> FemaleTags { get; set; }

        public virtual ICollection<ArtistTag> ArtistTags { get; set; }
        public virtual ICollection<GroupTag> GroupTags { get; set; }
        public virtual ICollection<SeriesTag> SeriesTags { get; set; }
        public virtual ICollection<CharacterTag> CharacterTags { get; set; }


        public virtual ICollection<ImageInfo> Files { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastDownloadTime { get; set; }
    }
}

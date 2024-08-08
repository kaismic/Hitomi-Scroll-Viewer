using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.Entities
{
    [Index(nameof(Title))]
    [Index(nameof(DownloadTime))]
    // TODO think about which one to index
    public class Gallery
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DownloadTime { get; set; }

        public int[] SceneIndexes { get; set; }

        private class ArtistsDictionaryConverter : JsonConverter<string[]>
        {
            public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                ICollection<string> result = [];
                if (reader.TokenType != JsonTokenType.StartArray) return [.. result];
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartObject:
                            break;
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

            public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
            {
                throw new InvalidOperationException("Gallery is not supposed to be serialized.");
            }
        }

        // Example: "artists":[{"artist":"hsd","url":"/artist/hsd-all.html"}, {"artist":"nora_higuma","url":"/artist/nora%20higuma-all.html"}]
        [JsonConverter(typeof(ArtistsDictionaryConverter))]
        public string[] Artists { get; set; }
        public ImageInfo[] Files { get; private set; } = [];
    }
}

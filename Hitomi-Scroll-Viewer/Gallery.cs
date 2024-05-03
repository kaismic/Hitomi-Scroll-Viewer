using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Hitomi_Scroll_Viewer {
    public class IntToStringConverter : System.Text.Json.Serialization.JsonConverter<string> {
        public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetInt32().ToString();

            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class Gallery {
        [System.Text.Json.Serialization.JsonConverter(typeof(IntToStringConverter))]
        public string id;
        public string title;
        public string japanese_title;
        public string type;
        public string language;
        public string language_url;
        public string language_localname;
        public int[] related;
        public string videofilename;
        public Dictionary<string, string>[] groups;
        public int[] scene_indexes;
        public Dictionary<string, string>[] artists;
        public Languages[] languages;
        public object video;
        public Dictionary<string, string>[] characters;
        public string date;
        public Dictionary<string, string>[] parodys;
        public Dictionary<string, object>[] tags;
        public ImageInfo[] files;

        public Gallery() {}

        public string GetArtists() {
            if (artists == null) {
                return null;
            }
            string result = "";
            for (int i = 0; i < artists.Length; i++) {
                result += artists[i]["artist"] + ", ";
            }
            return result[..^", ".Length];
        }
    }

    public struct ImageInfo {
        public string name;
        public int height;
        public int width;
        public int haswebp;
        public int hasavif;
        public int hasjxl;
        public string hash;

        public ImageInfo() {}
    }

    public struct Languages {
        public string url;
        public string name;
        [System.Text.Json.Serialization.JsonConverter(typeof(IntToStringConverter))]
        public string galleryid;
        public string language_localname;
        public Languages() { }
    }
}

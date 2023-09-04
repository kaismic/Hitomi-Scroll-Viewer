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
        public string type;
        public string language_localname;
        public string videofilename;
        public string language;
        [System.Text.Json.Serialization.JsonConverter(typeof(IntToStringConverter))]
        public string id;
        public ImageInfo[] files;
        public int[] related;
        public string title;
        public string japanese_title;
        public Dictionary<string, string>[] groups;
        public string language_url;
        public int[] scene_indexes;
        public Dictionary<string, string>[] artists;
        public Dictionary<string, string>[] languages;
        public object video;
        public Dictionary<string, string>[] characters;
        public string date;
        public Dictionary<string, string>[] parodys;
        public Dictionary<string, object>[] tags;

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

    public class ImageInfo {
        public string name;
        public int height;
        public int width;
        public int haswebp;
        public int hasavif;
        public int hasjxl;
        public string hash;

        public ImageInfo() {}
    }
}
